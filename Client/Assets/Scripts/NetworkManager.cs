using System;
using System.Collections.Generic;
using System.Net;
using DummyClient;
using ServerCore;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    ServerSession _session = new ServerSession();
    public bool IsConnected { get; private set; } = false;

    public void SetConnected(bool connected)
    {
        IsConnected = connected;
        Debug.Log($"[NETWORK] 연결 상태 변경: {connected}");
    }

    public void Send(ArraySegment<byte> sendBuff)
    {
        if (!IsConnected)
        {
            Debug.LogError("[NETWORK] 패킷 전송 실패 - 서버 연결 상태: DISCONNECTED");
            Debug.LogError($"[NETWORK] 세션 상태: {(_session != null ? "존재" : "null")}");
            return;
        }
        
        try
        {
            Debug.Log($"[NETWORK] 패킷 전송 시도 - 크기: {sendBuff.Count} bytes");
            _session.Send(sendBuff);
            Debug.Log("[NETWORK] 패킷 전송 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NETWORK] 패킷 전송 중 예외 발생: {e.Message}");
            Debug.LogError($"[NETWORK] 스택 트레이스: {e.StackTrace}");
        }
    }

    void Start()
    {
        Debug.Log("[NETWORK] NetworkManager Start() 시작");
        
        // IPv4 localhost로 연결 시도 (서버가 IPv4에서 리스닝 중)
        IPAddress ipAddr = IPAddress.Loopback; // 127.0.0.1
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

        Debug.Log($"[NETWORK] 서버 연결 시도 - IPv4 localhost (127.0.0.1:7777)");
        Debug.Log($"[NETWORK] 타겟 서버: {endPoint}");
        Debug.Log($"[NETWORK] 초기 연결 상태: IsConnected = {IsConnected}");

        Connector connector = new Connector();

        try
        {
            connector.Connect(
                endPoint,
                () =>
                {
                    Debug.Log("[NETWORK] 세션 팩토리 호출됨 - 세션 생성 중...");
                    return _session;
                },
                1
            );
            Debug.Log("[NETWORK] Connector.Connect() 호출 완료 - 비동기 연결 시작됨");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NETWORK] 연결 시도 중 예외 발생: {e.Message}");
            Debug.LogError($"[NETWORK] 스택 트레이스: {e.StackTrace}");
        }
    }

    void Update()
    {
        // 패킷 처리
        List<IPacket> list = PacketQueue.Instance.PopAll();
        if (list.Count > 0)
        {
            Debug.Log($"[NETWORK] {list.Count}개의 패킷 처리 중...");
        }

        foreach (IPacket packet in list)
            PacketManager.Instance.HandlePacket(_session, packet);

        // 연결 상태 모니터링 (5초마다)
        if (Time.time % 5.0f < 0.1f && Time.time > 1.0f)
        {
            CheckConnectionStatus();
        }
    }

    private void CheckConnectionStatus()
    {
        if (_session != null)
        {
            Debug.Log($"[NETWORK] 연결 상태 체크 - IsConnected: {IsConnected}");
            
            // IsConnected가 false인데 패킷을 보내려고 하는 상황 감지
            if (!IsConnected)
            {
                Debug.LogWarning("[NETWORK] 📍 문제 발견: 세션은 존재하지만 연결 상태가 FALSE!");
                Debug.LogWarning("[NETWORK] 📍 가능한 원인: 1) 연결 실패 2) 연결 후 끊어짐 3) OnConnected 콜백 미호출");
            }
            else
            {
                Debug.Log("[NETWORK] ✅ 연결 상태 정상");
            }
        }
        else
        {
            Debug.LogError("[NETWORK] ❌ 치명적 오류: 세션이 null입니다!");
        }
    }

    public void SendCMove(float x, float y, float z)
    {
        if (!IsConnected) return;
        var bytes = Game.FlatBuffersSupport.FlatMessageHelper.BuildCMove(x, y, z);
        Send(new ArraySegment<byte>(bytes));
    }

    public void SendCLeaveGame()
    {
        if (!IsConnected) return;
        var bytes = Game.FlatBuffersSupport.FlatMessageHelper.BuildCLeaveGame();
        Send(new ArraySegment<byte>(bytes));
    }
}
