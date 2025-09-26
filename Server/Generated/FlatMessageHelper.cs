using System;
using System.Collections.Generic;
using FlatBuffers;
using Game;

namespace Game.FlatBuffersSupport
{
    // Helper for building and parsing size‑prefixed FlatBuffer Message packets
    public static class FlatMessageHelper
    {
        // Build generic wrapper (size‑prefixed) returning byte[] ready to send
        private static byte[] FinishAndGetBytes(FlatBufferBuilder fbb, Offset<Message> msgOffset)
        {
            fbb.FinishSizePrefixed(msgOffset.Value, fileIdentifier: "GPKT");
            // Slice exact length (DataBuffer contains larger backing array)
            var buffer = fbb.DataBuffer; // ByteBuffer
            int start = buffer.Position; // points to size prefix start
            int len = buffer.Length - start;
            byte[] managed = new byte[len];
            Buffer.BlockCopy(buffer.Data, start, managed, 0, len);
            return managed;
        }

        private static Vec3 CreateVec3(FlatBufferBuilder fbb, float x, float y, float z)
        {
            return new Vec3(x, y, z);
        }

        // --------- Build Methods (Client -> Server) ---------
        public static byte[] BuildCMove(float x, float y, float z)
        {
            var fbb = new FlatBufferBuilder(128);
            var pos = CreateVec3(fbb, x, y, z);
            CMove.StartCMove(fbb);
            CMove.AddPos(fbb, pos);
            var cMoveOffset = CMove.EndCMove(fbb);

            Message.StartMessage(fbb);
            Message.AddType(fbb, PacketType.CMove);
            Message.AddCMove(fbb, cMoveOffset);
            var msgOffset = Message.EndMessage(fbb);
            return FinishAndGetBytes(fbb, msgOffset);
        }

        public static byte[] BuildCLeaveGame()
        {
            var fbb = new FlatBufferBuilder(32);
            CLeaveGame.StartCLeaveGame(fbb);
            var cg = CLeaveGame.EndCLeaveGame(fbb);
            Message.StartMessage(fbb);
            Message.AddType(fbb, PacketType.CLeaveGame);
            Message.AddCLeaveGame(fbb, cg);
            var msgOffset = Message.EndMessage(fbb);
            return FinishAndGetBytes(fbb, msgOffset);
        }

        // Legacy sample MoveReq / MoveRes build helpers (if still used)
        public static byte[] BuildMoveReq(int playerId, float x, float y, float z)
        {
            var fbb = new FlatBufferBuilder(128);
            var pos = CreateVec3(fbb, x, y, z);
            MoveReq.StartMoveReq(fbb);
            MoveReq.AddPlayerId(fbb, playerId);
            MoveReq.AddPos(fbb, pos);
            var mr = MoveReq.EndMoveReq(fbb);
            Message.StartMessage(fbb);
            Message.AddType(fbb, PacketType.MoveReq);
            Message.AddMoveReq(fbb, mr);
            var msgOffset = Message.EndMessage(fbb);
            return FinishAndGetBytes(fbb, msgOffset);
        }

        public static byte[] BuildMoveRes(int playerId, float x, float y, float z)
        {
            var fbb = new FlatBufferBuilder(128);
            var pos = CreateVec3(fbb, x, y, z);
            MoveRes.StartMoveRes(fbb);
            MoveRes.AddPlayerId(fbb, playerId);
            MoveRes.AddPos(fbb, pos);
            var mr = MoveRes.EndMoveRes(fbb);
            Message.StartMessage(fbb);
            Message.AddType(fbb, PacketType.MoveRes);
            Message.AddMoveRes(fbb, mr);
            var msgOffset = Message.EndMessage(fbb);
            return FinishAndGetBytes(fbb, msgOffset);
        }

        // --------- Build Methods (Server -> Client) ---------
        public static byte[] BuildSBroadcastEnterGame(int playerId, float x, float y, float z)
        {
            var fbb = new FlatBufferBuilder(128);
            var pos = CreateVec3(fbb, x, y, z);
            SBroadcastEnterGame.StartSBroadcastEnterGame(fbb);
            SBroadcastEnterGame.AddPlayerId(fbb, playerId);
            SBroadcastEnterGame.AddPos(fbb, pos);
            var off = SBroadcastEnterGame.EndSBroadcastEnterGame(fbb);
            Message.StartMessage(fbb);
            Message.AddType(fbb, PacketType.SBroadcastEnterGame);
            Message.AddSBroadcastEnterGame(fbb, off);
            var msgOffset = Message.EndMessage(fbb);
            return FinishAndGetBytes(fbb, msgOffset);
        }

        public static byte[] BuildSBroadcastLeaveGame(int playerId)
        {
            var fbb = new FlatBufferBuilder(64);
            SBroadcastLeaveGame.StartSBroadcastLeaveGame(fbb);
            SBroadcastLeaveGame.AddPlayerId(fbb, playerId);
            var off = SBroadcastLeaveGame.EndSBroadcastLeaveGame(fbb);
            Message.StartMessage(fbb);
            Message.AddType(fbb, PacketType.SBroadcastLeaveGame);
            Message.AddSBroadcastLeaveGame(fbb, off);
            var msgOffset = Message.EndMessage(fbb);
            return FinishAndGetBytes(fbb, msgOffset);
        }

        public static byte[] BuildSBroadcastMove(int playerId, float x, float y, float z)
        {
            var fbb = new FlatBufferBuilder(128);
            var pos = CreateVec3(fbb, x, y, z);
            SBroadcastMove.StartSBroadcastMove(fbb);
            SBroadcastMove.AddPlayerId(fbb, playerId);
            SBroadcastMove.AddPos(fbb, pos);
            var off = SBroadcastMove.EndSBroadcastMove(fbb);
            Message.StartMessage(fbb);
            Message.AddType(fbb, PacketType.SBroadcastMove);
            Message.AddSBroadcastMove(fbb, off);
            var msgOffset = Message.EndMessage(fbb);
            return FinishAndGetBytes(fbb, msgOffset);
        }

        public static byte[] BuildSPlayerList(IReadOnlyList<(bool isSelf, int playerId, float x, float y, float z)> entries)
        {
            var fbb = new FlatBufferBuilder(256);

            // Build PlayerEntry vector
            var playerOffsets = new Offset<PlayerEntry>[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var pos = CreateVec3(fbb, e.x, e.y, e.z);
                PlayerEntry.StartPlayerEntry(fbb);
                PlayerEntry.AddIsSelf(fbb, e.isSelf);
                PlayerEntry.AddPlayerId(fbb, e.playerId);
                PlayerEntry.AddPos(fbb, pos);
                playerOffsets[i] = PlayerEntry.EndPlayerEntry(fbb);
            }
            var vec = SPlayerList.CreatePlayersVector(fbb, playerOffsets);
            SPlayerList.StartSPlayerList(fbb);
            SPlayerList.AddPlayers(fbb, vec);
            var listOffset = SPlayerList.EndSPlayerList(fbb);

            Message.StartMessage(fbb);
            Message.AddType(fbb, PacketType.SPlayerList);
            Message.AddSPlayerList(fbb, listOffset);
            var msgOffset = Message.EndMessage(fbb);
            return FinishAndGetBytes(fbb, msgOffset);
        }

        // --------- Parse Methods ---------
        public static Message? Parse(byte[] raw, int offset = 0)
        {
            if (raw == null || raw.Length - offset < 4)
                return null; // not enough for size prefix

            // raw already includes size prefix because we used FinishSizePrefixed.
            var bb = new ByteBuffer(raw, offset);
            try
            {
                return Message.GetRootAsMessage(bb);
            }
            catch
            {
                return null;
            }
        }

        // Utility to extract which ID is set (debug aid)
        public static PacketType GetPacketType(Message msg) => msg.Type;
    }
}
