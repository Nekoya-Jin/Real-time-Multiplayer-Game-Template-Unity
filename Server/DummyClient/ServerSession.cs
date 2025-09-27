using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServerCore;
using Game; // FlatBuffers generated
using Game.FlatBuffersSupport;

namespace DummyClient
{
    class ServerSession : Session // PacketSession -> FlatBuffers size-prefixed Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Logger.LogInfo($"Client connected to server: {endPoint}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Logger.LogInfo($"Client disconnected from server: {endPoint}");
        }

        private static string BytesToHex(byte[] arr, int offset, int count, int max = 32)
        {
            int len = System.Math.Min(count, max);
            StringBuilder sb = new StringBuilder(len * 2);
            for (int i = 0; i < len; i++) sb.AppendFormat("{0:X2}", arr[offset + i]);
            if (count > len) sb.Append("...");
            return sb.ToString();
        }

        public override int OnRecv(ArraySegment<byte> buffer)
        {
            int processed = 0;
            while (true)
            {
                if (buffer.Count - processed < 4)
                    break;
                int msgLen = System.BitConverter.ToInt32(buffer.Array!, buffer.Offset + processed);
                if (msgLen <= 0 || msgLen > 1024 * 128)
                {
                    Logger.LogError($"[CLIENT] Invalid msgLen={msgLen} remaining={buffer.Count - processed}");
                    Disconnect();
                    return processed;
                }
                if (buffer.Count - processed < 4 + msgLen)
                    break;
                var segment = new ArraySegment<byte>(buffer.Array!, buffer.Offset + processed, 4 + msgLen);
                Logger.LogDebug($"[CLIENT RECV] total={segment.Count} len={msgLen} hex={BytesToHex(segment.Array!, segment.Offset, segment.Count)}");
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
                case PacketType.SBroadcastMove:
                    {
                        var mNullable = msg.Value.SBroadcastMove;
                        if (mNullable.HasValue)
                        {
                            var m = mNullable.Value;
                            var posNullable = m.Pos;
                            if (posNullable.HasValue)
                            {
                                var pos = posNullable.Value;
                                Logger.LogDebug($"Broadcast Move Player={m.PlayerId} Pos=({pos.X},{pos.Y},{pos.Z})");
                            }
                        }
                        break;
                    }
                case PacketType.SBroadcastEnterGame:
                    {
                        var egNullable = msg.Value.SBroadcastEnterGame;
                        if (egNullable.HasValue)
                        {
                            var eg = egNullable.Value;
                            var posNullable = eg.Pos;
                            if (posNullable.HasValue)
                            {
                                var pos = posNullable.Value;
                                Logger.LogInfo($"Player Enter Player={eg.PlayerId} Pos=({pos.X},{pos.Y},{pos.Z})");
                            }
                        }
                        break;
                    }
                case PacketType.SBroadcastLeaveGame:
                    {
                        var lgNullable = msg.Value.SBroadcastLeaveGame;
                        if (lgNullable.HasValue)
                        {
                            var lg = lgNullable.Value;
                            Logger.LogInfo($"Player Leave Player={lg.PlayerId}");
                        }
                        break;
                    }
                case PacketType.SPlayerList:
                    {
                        var listNullable = msg.Value.SPlayerList;
                        if (listNullable.HasValue)
                        {
                            var list = listNullable.Value;
                            int len = list.PlayersLength;
                            for (int i = 0; i < len; i++)
                            {
                                var entryNullable = list.Players(i);
                                if (entryNullable.HasValue)
                                {
                                    var entry = entryNullable.Value;
                                    var posNullable = entry.Pos;
                                    if (posNullable.HasValue)
                                    {
                                        var pos = posNullable.Value;
                                        Logger.LogInfo($"PlayerList Entry Player={entry.PlayerId} Self={entry.IsSelf} Pos=({pos.X},{pos.Y},{pos.Z})");
                                    }
                                }
                            }
                        }
                        break;
                    }
                case PacketType.MoveRes:
                    {
                        var mrNullable = msg.Value.MoveRes;
                        if (mrNullable.HasValue)
                        {
                            var mr = mrNullable.Value;
                            var posNullable = mr.Pos;
                            if (posNullable.HasValue)
                            {
                                var pos = posNullable.Value;
                                Logger.LogDebug($"MoveRes Player={mr.PlayerId} Pos=({pos.X},{pos.Y},{pos.Z})");
                            }
                        }
                        break;
                    }
                default:
                    Logger.LogWarning($"Unhandled packet type: {msg.Value.Type}");
                    break;
            }
        }

        public override void OnSend(int numOfBytes) { }
    }
}
