using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using ServerCore;
using Game; // FlatBuffers generated
using Game.FlatBuffersSupport; // FlatMessageHelper

namespace DummyClient
{
    class SessionManager
    {
        static SessionManager _session = new SessionManager();
        public static SessionManager Instance => _session;

        private readonly ConcurrentBag<ServerSession> _sessions = new ConcurrentBag<ServerSession>();
        private readonly Random _rand = new Random();

        public void SendForEach()
        {
            foreach (var session in _sessions)
            {
                try
                {
                    // 기존 C_Move 구조체 -> FlatBuffers CMove 메시지 전송으로 변경
                    float x = _rand.Next(-50, 50);
                    float y = 0f;
                    float z = _rand.Next(-50, 50);
                    var bytes = FlatMessageHelper.BuildCMove(x, y, z);
                    session.Send(new ArraySegment<byte>(bytes));
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error sending packet: {ex.Message}");
                }
            }
        }

        public ServerSession Generate()
        {
            var session = new ServerSession();
            _sessions.Add(session);
            Logger.LogDebug("New client session generated");
            return session;
        }
    }
}
