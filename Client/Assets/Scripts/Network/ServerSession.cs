using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServerCore;
using Game;
using Game.FlatBuffersSupport;

namespace DummyClient
{
    class ServerSession : Session // PacketSession -> Session (FlatBuffers 직접 파싱)
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");
            UnityEngine.Debug.Log($"[CONNECTION] 서버 연결 성공! - {endPoint}");
            var networkManager = UnityEngine.GameObject.FindFirstObjectByType<NetworkManager>();
            if (networkManager != null)
                networkManager.SetConnected(true);
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
            UnityEngine.Debug.Log($"[CONNECTION] 서버 연결 해제 - {endPoint}");
            var networkManager = UnityEngine.GameObject.FindFirstObjectByType<NetworkManager>();
            if (networkManager != null)
                networkManager.SetConnected(false);
        }

        public override int OnRecv(ArraySegment<byte> buffer)
        {
            int processed = 0;
            while (true)
            {
                if (buffer.Count - processed < 4)
                    break;
                int msgLen = BitConverter.ToInt32(buffer.Array, buffer.Offset + processed);
                if (buffer.Count - processed < 4 + msgLen)
                    break;
                var segment = new ArraySegment<byte>(buffer.Array, buffer.Offset + processed, 4 + msgLen);
                OnRecvPacket(segment);
                processed += 4 + msgLen;
            }
            return processed;
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            var msg = FlatMessageHelper.Parse(buffer.Array, buffer.Offset);
            if (msg == null) return;
            switch (msg.Value.Type)
            {
                case PacketType.SBroadcastEnterGame:
                {
                    var pkt = msg.Value.SBroadcastEnterGame.Value;
                    UnityEngine.Debug.Log($"[PKT] EnterGame player={pkt.PlayerId}");
                    break;
                }
                case PacketType.SBroadcastLeaveGame:
                {
                    var pkt = msg.Value.SBroadcastLeaveGame.Value;
                    UnityEngine.Debug.Log($"[PKT] LeaveGame player={pkt.PlayerId}");
                    break;
                }
                case PacketType.SPlayerList:
                {
                    var list = msg.Value.SPlayerList.Value;
                    int count = list.PlayersLength;
                    UnityEngine.Debug.Log($"[PKT] PlayerList count={count}");
                    for (int i = 0; i < count; i++)
                    {
                        var entry = list.Players(i).Value;
                        UnityEngine.Debug.Log($"  - playerId={entry.PlayerId} isSelf={entry.IsSelf}");
                    }
                    break;
                }
                case PacketType.SBroadcastMove:
                {
                    var mv = msg.Value.SBroadcastMove.Value;
                    UnityEngine.Debug.Log($"[PKT] Move player={mv.PlayerId}");
                    break;
                }
                default:
                    UnityEngine.Debug.Log($"[PKT] Unhandled type {msg.Value.Type}");
                    break;
            }
        }

        public override void OnSend(int numOfBytes)
        {
            UnityEngine.Debug.Log($"[SEND] {numOfBytes} bytes 전송 완료");
        }
    }
}
