using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ServerCore;

namespace Server
{
    class SessionManager
    {
        static SessionManager _session = new SessionManager();
        public static SessionManager Instance
        {
            get { return _session; }
        }

        private int _sessionId = 0;
        private readonly ConcurrentDictionary<int, ClientSession> _sessions =
            new ConcurrentDictionary<int, ClientSession>();

        public ClientSession Generate()
        {
            int sessionId = Interlocked.Increment(ref _sessionId);

            var session = new ClientSession();
            session.SessionId = sessionId;

            if (_sessions.TryAdd(sessionId, session))
            {
                Logger.LogInfo($"Client connected: {sessionId}");
                return session;
            }
            else
            {
                Logger.LogError($"Failed to add session: {sessionId}");
                return null;
            }
        }

        public ClientSession Find(int id)
        {
            _sessions.TryGetValue(id, out var session);
            return session;
        }

        public void Remove(ClientSession session)
        {
            if (_sessions.TryRemove(session.SessionId, out var removedSession))
            {
                Logger.LogInfo($"Client disconnected: {session.SessionId}");
            }
        }

        /// <summary>
        /// 모든 활성 세션 수 반환
        /// </summary>
        public int ActiveSessionCount => _sessions.Count;

        /// <summary>
        /// 모든 세션에 대해 작업 실행
        /// </summary>
        public void ForEachSession(Action<ClientSession> action)
        {
            foreach (var kvp in _sessions)
            {
                try
                {
                    action(kvp.Value);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error processing session {kvp.Key}: {ex.Message}");
                }
            }
        }
    }
}
