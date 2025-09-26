using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPlayer : Player
{
    NetworkManager _network;

    void Start()
    {
        Debug.Log("[MYPLAYER] MyPlayer 시작");
        _network = GameObject.Find("NetworkManager")?.GetComponent<NetworkManager>();
        StartCoroutine("CoSendPacket");
    }

    void Update() { }

    IEnumerator CoSendPacket()
    {
        // 연결될 때까지 대기
        Debug.Log("[MYPLAYER] 서버 연결 대기 중...");
        while (_network == null || !_network.IsConnected)
        {
            yield return new WaitForSeconds(0.1f);
            if (_network == null)
            {
                _network = GameObject.Find("NetworkManager")?.GetComponent<NetworkManager>();
            }
        }
        
        Debug.Log("[MYPLAYER] ✅ 서버 연결 완료! 패킷 전송 시작");

        while (true)
        {
            yield return new WaitForSeconds(0.25f);

            // 연결 상태 재확인
            if (_network == null || !_network.IsConnected)
            {
                Debug.LogWarning("[MYPLAYER] ⚠️ 연결이 끊어짐! 재연결 대기 중...");
                continue;
            }

            C_Move movePacket = new C_Move();
            movePacket.posX = UnityEngine.Random.Range(-50, 50);
            movePacket.posY = 0;
            movePacket.posZ = UnityEngine.Random.Range(-50, 50);

            Debug.Log(
                $"[SEND] C_Move 패킷 전송 - 위치: ({movePacket.posX}, {movePacket.posY}, {movePacket.posZ})"
            );
            _network.Send(movePacket.Write());
        }
    }
}
