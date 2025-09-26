using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ServerCore
{
    /// <summary>
    /// 네트워크 관련 공통 유틸리티 클래스
    /// </summary>
    public static class NetworkHelper
    {
        /// <summary>
        /// Socket 에러가 치명적인지 확인
        /// </summary>
        public static bool IsFatalError(SocketError error)
        {
            return error != SocketError.Success
                && error != SocketError.WouldBlock
                && error != SocketError.IOPending;
        }

        /// <summary>
        /// 안전하게 Socket을 종료
        /// </summary>
        public static void SafeCloseSocket(Socket socket)
        {
            if (socket == null)
                return;

            try
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during socket shutdown: {ex.Message}");
            }
            finally
            {
                try
                {
                    socket.Close();
                    socket.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error during socket disposal: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Socket 연결 상태 확인
        /// </summary>
        public static bool IsSocketConnected(Socket socket)
        {
            if (socket == null)
                return false;

            try
            {
                return socket.Connected && !socket.Poll(1000, SelectMode.SelectRead)
                    || socket.Available > 0;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 간단한 로거 클래스
    /// </summary>
    public static class Logger
    {
        public static void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public static void LogWarning(string message)
        {
            Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public static void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public static void LogDebug(string message)
        {
#if DEBUG
            Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
#endif
        }
    }
}
