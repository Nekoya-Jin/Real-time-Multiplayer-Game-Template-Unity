using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ServerCore;

namespace DummyClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Logger.LogInfo("DummyClient starting up...");

                // 서버 연결 설정
                const int serverPort = 7777;
                const int clientCount = 5;

                var endPoint = new IPEndPoint(IPAddress.Loopback, serverPort);
                var connector = new Connector();

                Logger.LogInfo($"Connecting {clientCount} clients to {endPoint}");

                // 서버에 연결 (성공 개수 반영)
                int success = connector.Connect(
                    endPoint,
                    () => SessionManager.Instance.Generate(),
                    clientCount
                );
                if (success == clientCount)
                    Logger.LogInfo("All clients connected successfully");
                else if (success == 0)
                    Logger.LogWarning("No clients connected");
                else
                    Logger.LogWarning($"Only {success}/{clientCount} clients connected");

                // 클라이언트 메인 루프
                Logger.LogInfo("Client message loop started");
                while (true)
                {
                    try
                    {
                        SessionManager.Instance.SendForEach();
                        Thread.Sleep(250);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Error in send loop: {e.Message}");
                        Thread.Sleep(1000); // 에러 발생 시 잠시 대기
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"DummyClient failed: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
