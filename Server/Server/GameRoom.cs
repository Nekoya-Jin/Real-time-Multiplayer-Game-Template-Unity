using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;
using Game;
using Game.FlatBuffersSupport;

namespace Server
{
    class GameRoom : IJobQueue
    {
        List<ClientSession> _sessions = new List<ClientSession>();
        JobQueue _jobQueue = new JobQueue();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        public void Push(Action job) => _jobQueue.Push(job);

        public void Flush()
        {
            if (_pendingList.Count == 0)
                return;
            foreach (ClientSession s in _sessions)
                s.Send(_pendingList);
            _pendingList.Clear();
        }

        public void Broadcast(byte[] raw)
        {
            _pendingList.Add(new ArraySegment<byte>(raw));
        }

        public void Broadcast(ArraySegment<byte> segment)
        {
            _pendingList.Add(segment);
        }

        public void Enter(ClientSession session)
        {
            _sessions.Add(session);
            session.Room = this;

            // PlayerList 전송 (자신 포함 모든 플레이어)
            var entries = new List<(bool isSelf, int playerId, float x, float y, float z)>(_sessions.Count);
            foreach (ClientSession s in _sessions)
            {
                entries.Add((s == session, s.SessionId, s.PosX, s.PosY, s.PosZ));
            }
            var playerListBytes = FlatMessageHelper.BuildSPlayerList(entries);
            session.Send(new ArraySegment<byte>(playerListBytes));

            // EnterGame broadcast
            var enterBytes = FlatMessageHelper.BuildSBroadcastEnterGame(session.SessionId, session.PosX, session.PosY, session.PosZ);
            Broadcast(enterBytes);
        }

        public void Leave(ClientSession session)
        {
            _sessions.Remove(session);
            var leaveBytes = FlatMessageHelper.BuildSBroadcastLeaveGame(session.SessionId);
            Broadcast(leaveBytes);
        }

        public void Move(ClientSession session, float x, float y, float z)
        {
            session.PosX = x;
            session.PosY = y;
            session.PosZ = z;

            var moveBytes = FlatMessageHelper.BuildSBroadcastMove(session.SessionId, session.PosX, session.PosY, session.PosZ);
            Broadcast(moveBytes);
        }
    }
}
