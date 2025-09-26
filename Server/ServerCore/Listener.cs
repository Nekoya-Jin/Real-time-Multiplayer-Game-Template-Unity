using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Listener
    {
        private Socket _listenSocket;
        private Func<Session> _sessionFactory;
        private bool _isRunning;

        public void Init(
            IPEndPoint endPoint,
            Func<Session> sessionFactory,
            int register = 10,
            int backlog = 100
        )
        {
            _sessionFactory = sessionFactory;
            _isRunning = true;

            // Socket 생성 및 재사용 옵션 설정
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true
            );

            try
            {
                // Socket 바인딩 및 리스닝 시작
                _listenSocket.Bind(endPoint);
                _listenSocket.Listen(backlog);

                Logger.LogInfo($"Server listening on {endPoint}");

                // 비동기 Accept 작업 시작
                for (int i = 0; i < register; i++)
                {
                    StartAcceptAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize listener: {ex.Message}");
                throw;
            }
        }

        private async void StartAcceptAsync()
        {
            while (_isRunning && _listenSocket != null)
            {
                try
                {
                    var acceptedSocket = await AcceptSocketAsync();
                    if (acceptedSocket != null)
                    {
                        _ = Task.Run(() => ProcessAcceptedConnection(acceptedSocket));
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Listener가 정리된 경우
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Accept error: {ex.Message}");
                    await Task.Delay(100); // 잠시 대기 후 재시도
                }
            }
        }

        private Task<Socket> AcceptSocketAsync()
        {
            return Task.Factory.FromAsync(_listenSocket.BeginAccept, _listenSocket.EndAccept, null);
        }

        private void ProcessAcceptedConnection(Socket acceptedSocket)
        {
            try
            {
                var session = _sessionFactory?.Invoke();
                if (session != null)
                {
                    session.Start(acceptedSocket);
                    session.OnConnected(acceptedSocket.RemoteEndPoint);
                }
                else
                {
                    acceptedSocket?.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error processing accepted connection: {ex.Message}");
                acceptedSocket?.Close();
            }
        }

        public void Stop()
        {
            _isRunning = false;

            try
            {
                _listenSocket?.Close();
                _listenSocket?.Dispose();
                _listenSocket = null;

                Logger.LogInfo("Listener stopped successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error stopping listener: {ex.Message}");
            }
        }
    }
}
