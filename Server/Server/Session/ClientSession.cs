using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using Game; // FlatBuffers generated
using Game.FlatBuffersSupport;

namespace Server
{
    class ClientSession : Session // 변경: PacketSession -> Session (FlatBuffers 직접 파싱)
    {
        public int SessionId { get; set; }
        public GameRoom Room { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }

        public override void OnConnected(EndPoint endPoint)
        {
            Logger.LogInfo($"Client connected (SessionId={SessionId}, EndPoint={endPoint})");

            Program.Room.Push(() => Program.Room.Enter(this));
        }

        // FlatBuffers size-prefixed 패킷 수신 처리
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
                case PacketType.CMove:
                    {
                        var cMove = msg.Value.CMove.Value;
                        var pos = cMove.Pos.Value;
                        Room?.Push(() => Room.Move(this, pos.X, pos.Y, pos.Z));
                        break;
                    }
                case PacketType.CLeaveGame:
                    {
                        Room?.Push(() => Room.Leave(this));
                        break;
                    }
                case PacketType.MoveReq:
                    {
                        var req = msg.Value.MoveReq.Value;
                        var pos = req.Pos.Value;
                        var send = FlatMessageHelper.BuildMoveRes(req.PlayerId, pos.X, pos.Y, pos.Z);
                        Send(new ArraySegment<byte>(send));
                        break;
                    }
                default:
                    break;
            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            SessionManager.Instance.Remove(this);
            if (Room != null)
            {
                GameRoom room = Room;
                room.Push(() => room.Leave(this));
                Room = null;
            }
            Logger.LogInfo($"Client disconnected (SessionId={SessionId}, EndPoint={endPoint})");
        }

        public override void OnSend(int numOfBytes) { }
    }
}
