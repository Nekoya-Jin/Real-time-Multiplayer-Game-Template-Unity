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
    class ClientSession : Session // Session 기반 FlatBuffers 직접 파싱
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

        // size-prefixed FlatBuffers 패킷 수신 처리
        public override int OnRecv(ArraySegment<byte> buffer)
        {
            int processed = 0;
            while (true)
            {
                if (buffer.Count - processed < 4)
                    break;
                int msgLen = BitConverter.ToInt32(buffer.Array!, buffer.Offset + processed);
                if (msgLen <= 0 || msgLen > 1024 * 128) // sanity limit 128KB
                {
                    Logger.LogError($"Invalid msgLen={msgLen} remaining={buffer.Count - processed}");
                    Disconnect();
                    return processed;
                }
                if (buffer.Count - processed < 4 + msgLen)
                    break; // wait more
                var segment = new ArraySegment<byte>(buffer.Array!, buffer.Offset + processed, 4 + msgLen);
                Logger.LogDebug($"[RECV] total={segment.Count} len={msgLen} hex={BytesToHex(segment.Array!, segment.Offset, segment.Count)}");
                HandlePacket(segment);
                processed += 4 + msgLen;
            }
            return processed;
        }

        private void HandlePacket(ArraySegment<byte> buffer)
        {
            var msg = FlatMessageHelper.Parse(buffer.Array!, buffer.Offset);
            if (msg == null)
                return;

            switch (msg.Value.Type)
            {
                case PacketType.CMove:
                    {
                        var cMoveNullable = msg.Value.CMove;
                        if (cMoveNullable.HasValue)
                        {
                            var cMove = cMoveNullable.Value;
                            var posNullable = cMove.Pos;
                            if (posNullable.HasValue)
                            {
                                var pos = posNullable.Value;
                                Room?.Push(() => Room.Move(this, pos.X, pos.Y, pos.Z));
                            }
                        }
                        break;
                    }
                case PacketType.CLeaveGame:
                    {
                        Room?.Push(() => Room.Leave(this));
                        break;
                    }
                case PacketType.MoveReq:
                    {
                        var reqNullable = msg.Value.MoveReq;
                        if (reqNullable.HasValue)
                        {
                            var req = reqNullable.Value;
                            var posNullable = req.Pos;
                            if (posNullable.HasValue)
                            {
                                var pos = posNullable.Value;
                                var send = FlatMessageHelper.BuildMoveRes(req.PlayerId, pos.X, pos.Y, pos.Z);
                                Send(new ArraySegment<byte>(send));
                            }
                        }
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

        private static string BytesToHex(byte[] arr, int offset, int count, int max = 32)
        {
            int len = Math.Min(count, max);
            StringBuilder sb = new StringBuilder(len * 2);
            for (int i = 0; i < len; i++) sb.AppendFormat("{0:X2}", arr[offset + i]);
            if (count > len) sb.Append("...");
            return sb.ToString();
        }
    }
}
