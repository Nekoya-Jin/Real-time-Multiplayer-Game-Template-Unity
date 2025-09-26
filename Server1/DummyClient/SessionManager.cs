using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using ServerCore;

namespace DummyClient
{
    class SessionManager
    {
        static SessionManager _session = new SessionManager();
        public static SessionManager Instance
        {
            get { return _session; }
        }

        private readonly ConcurrentBag<ServerSession> _sessions =
            new ConcurrentBag<ServerSession>();
        private readonly Random _rand = new Random();

        public void SendForEach()
        {
            foreach (var session in _sessions)
            {
                try
                {
                    var movePacket = new C_Move();
                    movePacket.posX = _rand.Next(-50, 50);
                    movePacket.posY = 0;
                    movePacket.posZ = _rand.Next(-50, 50);
                    session.Send(movePacket.Write());
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
