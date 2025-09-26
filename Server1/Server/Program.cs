using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;

namespace Server
{
    class Program
    {
        static Listener _listener = new Listener();
        public static GameRoom Room = new GameRoom();

        static void FlushRoom()
        {
            Room.Push(() => Room.Flush());
            JobTimer.Instance.Push(FlushRoom, 250, "RoomFlush");
        }

        static void Main(string[] args)
        {
            try
            {
                Logger.LogInfo("Server starting up...");

                // 서버 설정
                const int serverPort = 7777;
                var endPoint = new IPEndPoint(IPAddress.Any, serverPort);

                // 리스너 초기화
                _listener.Init(endPoint, () => SessionManager.Instance.Generate());

                Logger.LogInfo($"Server listening on port {serverPort}");

                // 게임룸 플러시 작업 시작
                JobTimer.Instance.Push(FlushRoom, 0, "InitialRoomFlush");

                // 서버 메인 루프
                Logger.LogInfo("Server main loop started");
                while (true)
                {
                    JobTimer.Instance.Flush();
                    Thread.Sleep(1); // CPU 사용량 제한
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Server startup failed: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                _listener?.Stop();
                Logger.LogInfo("Server shutdown complete");
            }
        }
    }
}
