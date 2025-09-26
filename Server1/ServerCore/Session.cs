using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;

        // [size(2)][packetId(2)][ ... ][size(2)][packetId(2)][ ... ]
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;
            int packetCount = 0;

            while (true)
            {
                // 최소한 헤더는 파싱할 수 있는지 확인
                if (buffer.Count < HeaderSize)
                    break;

                // 패킷이 완전체로 도착했는지 확인
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < dataSize)
                    break;

                // 여기까지 왔으면 패킷 조립 가능
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));
                packetCount++;

                processLen += dataSize;
                buffer = new ArraySegment<byte>(
                    buffer.Array,
                    buffer.Offset + dataSize,
                    buffer.Count - dataSize
                );
            }

            return processLen;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }

    public abstract class Session
    {
        Socket _socket;
        int _disconnected = 0;

        // 가독성을 위한 상태 상수 (매직 넘버 제거)
        private const int Connected = 0;
        private const int Disconnected = 1;

        private const int SendIdle = 0;
        private const int SendPending = 1;

        RecvBuffer _recvBuffer = new RecvBuffer(65535);

        private readonly ConcurrentQueue<ArraySegment<byte>> _sendQueue =
            new ConcurrentQueue<ArraySegment<byte>>();
        private int _pendingSend = 0;
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);

        void Clear()
        {
            // 전송 큐 비우기
            while (_sendQueue.TryDequeue(out _)) { }

            // 전송 상태 초기화
            Interlocked.Exchange(ref _pendingSend, SendIdle);

            // 이벤트 핸들러 해제(세션 참조 누수 방지)
            _recvArgs.Completed -= OnRecvCompleted;
            _sendArgs.Completed -= OnSendCompleted;

            // 버퍼/상태 초기화
            _sendArgs.BufferList = null; // SetBuffer(null, ..)은 허용되지 않으므로 생략
            _recvBuffer.Clean();
        }

        public void Start(Socket socket)
        {
            _socket = socket;

            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        public void Send(List<ArraySegment<byte>> sendBuffList)
        {
            if (sendBuffList.Count == 0)
                return;

            foreach (ArraySegment<byte> sendBuff in sendBuffList)
                _sendQueue.Enqueue(sendBuff);

            // 원자적으로 _pendingSend 상태 확인 후 전송 시작 (Exchange로 가독성 향상)
            if (Interlocked.Exchange(ref _pendingSend, SendPending) == SendIdle)
                RegisterSend();
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            _sendQueue.Enqueue(sendBuff);

            // 원자적으로 _pendingSend 상태 확인 후 전송 시작 (Exchange로 가독성 향상)
            if (Interlocked.Exchange(ref _pendingSend, SendPending) == SendIdle)
                RegisterSend();
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, Disconnected) == Disconnected)
                return;

            try
            {
                OnDisconnected(_socket?.RemoteEndPoint);
                NetworkHelper.SafeCloseSocket(_socket);
            }
            finally
            {
                Clear();
                _socket = null;
            }
        }

        #region 네트워크 통신

        void RegisterSend()
        {
            if (_disconnected == Disconnected)
            {
                Interlocked.Exchange(ref _pendingSend, SendIdle);
                return;
            }

            List<ArraySegment<byte>> bufferList = new List<ArraySegment<byte>>();

            // ConcurrentQueue에서 모든 항목 추출
            while (_sendQueue.TryDequeue(out ArraySegment<byte> buff))
            {
                bufferList.Add(buff);
            }

            if (bufferList.Count == 0)
            {
                Interlocked.Exchange(ref _pendingSend, SendIdle);
                return;
            }

            _sendArgs.BufferList = bufferList;

            try
            {
                bool pending = _socket.SendAsync(_sendArgs);
                if (pending == false)
                    OnSendCompleted(null, _sendArgs);
            }
            catch (Exception e)
            {
                Logger.LogError($"RegisterSend Failed: {e.Message}");
                Interlocked.Exchange(ref _pendingSend, SendIdle);
                Disconnect();
            }
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    _sendArgs.BufferList = null;

                    OnSend(_sendArgs.BytesTransferred);

                    // 큐에 더 보낼 데이터가 있으면 계속 전송, 없으면 pending 상태 해제
                    if (_sendQueue.IsEmpty)
                    {
                        Interlocked.Exchange(ref _pendingSend, SendIdle);
                    }
                    else
                    {
                        RegisterSend();
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError($"OnSendCompleted Failed: {e.Message}");
                    Interlocked.Exchange(ref _pendingSend, SendIdle);
                    Disconnect();
                }
            }
            else
            {
                Interlocked.Exchange(ref _pendingSend, SendIdle);
                Disconnect();
            }
        }

        void RegisterRecv()
        {
            if (_disconnected == Disconnected)
                return;

            _recvBuffer.Clean();
            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            try
            {
                bool pending = _socket.ReceiveAsync(_recvArgs);
                if (pending == false)
                    OnRecvCompleted(null, _recvArgs);
            }
            catch (Exception e)
            {
                Logger.LogError($"RegisterRecv Failed: {e.Message}");
                Disconnect();
            }
        }

        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    // Write 커서 이동
                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        Disconnect();
                        return;
                    }

                    // 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
                    int processLen = OnRecv(_recvBuffer.ReadSegment);
                    if (processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        Disconnect();
                        return;
                    }

                    // Read 커서 이동
                    if (_recvBuffer.OnRead(processLen) == false)
                    {
                        Disconnect();
                        return;
                    }

                    RegisterRecv();
                }
                catch (Exception e)
                {
                    Logger.LogError($"OnRecvCompleted Failed: {e.Message}");
                    Disconnect();
                }
            }
            else
            {
                Disconnect();
            }
        }

        #endregion
    }
}
