using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Connector
    {
        private Func<Session> _sessionFactory;

        public async Task<int> ConnectAsync(
            IPEndPoint endPoint,
            Func<Session> sessionFactory,
            int count = 1,
            int maxRetries = 5,
            int retryDelayMs = 300
        )
        {
            _sessionFactory = sessionFactory;
            var tasks = new List<Task<bool>>();

            Logger.LogInfo(
                $"Connecting {count} clients to {endPoint} (retries={maxRetries}, delay={retryDelayMs}ms)"
            );

            for (int i = 0; i < count; i++)
            {
                tasks.Add(ConnectSingleAsync(endPoint, i, maxRetries, retryDelayMs));
            }

            var results = await Task.WhenAll(tasks);
            int success = 0;
            foreach (var r in results)
            {
                if (r)
                    success++;
            }

            if (success == count)
                Logger.LogInfo($"Connector summary: {success}/{count} clients connected.");
            else if (success == 0)
                Logger.LogWarning($"Connector summary: {success}/{count} clients connected.");
            else
                Logger.LogWarning($"Connector summary: {success}/{count} clients connected.");

            return success;
        }

        private async Task<bool> ConnectSingleAsync(
            IPEndPoint endPoint,
            int connectionId,
            int maxRetries,
            int retryDelayMs
        )
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                Socket socket = null;
                try
                {
                    socket = new Socket(
                        endPoint.AddressFamily,
                        SocketType.Stream,
                        ProtocolType.Tcp
                    );

                    Logger.LogDebug(
                        $"Attempting connection {connectionId} to {endPoint} (try {attempt}/{maxRetries})"
                    );

                    await socket.ConnectAsync(endPoint);

                    var session = _sessionFactory?.Invoke();
                    if (session != null)
                    {
                        session.Start(socket);
                        session.OnConnected(endPoint);
                        Logger.LogInfo($"Connection {connectionId} established");
                        return true;
                    }
                    else
                    {
                        socket.Close();
                        Logger.LogError(
                            $"Connection {connectionId} failed: Session factory returned null"
                        );
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        $"Connection {connectionId} failed on attempt {attempt}: {ex.Message}"
                    );
                    try
                    {
                        socket?.Close();
                    }
                    catch { }

                    if (attempt < maxRetries)
                        await Task.Delay(retryDelayMs);
                    else
                        return false;
                }
            }

            return false;
        }

        // 기존 동기 메서드 유지 (하위 호환성)
        public int Connect(IPEndPoint endPoint, Func<Session> sessionFactory, int count = 1)
        {
            return ConnectAsync(endPoint, sessionFactory, count).GetAwaiter().GetResult();
        }
    }
}
