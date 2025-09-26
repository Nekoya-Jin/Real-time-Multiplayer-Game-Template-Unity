using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServerCore;

namespace DummyClient
{
    class ServerSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");
            UnityEngine.Debug.Log($"[CONNECTION] 서버 연결 성공! - {endPoint}");
            
            // NetworkManager의 연결 상태 업데이트
            var networkManager = UnityEngine.GameObject.FindFirstObjectByType<NetworkManager>();
            if (networkManager != null)
            {
                networkManager.SetConnected(true);
            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
            UnityEngine.Debug.Log($"[CONNECTION] 서버 연결 해제 - {endPoint}");
            
            // NetworkManager의 연결 상태 업데이트
            var networkManager = UnityEngine.GameObject.FindFirstObjectByType<NetworkManager>();
            if (networkManager != null)
            {
                networkManager.SetConnected(false);
            }
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnRecvPacket(this, buffer, (s, p) => PacketQueue.Instance.Push(p));
        }

        public override void OnSend(int numOfBytes)
        {
            UnityEngine.Debug.Log($"[SEND] {numOfBytes} bytes 전송 완료");
        }
    }
}
