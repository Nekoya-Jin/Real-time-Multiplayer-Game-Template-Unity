using System;
using System.Collections.Generic;
using System.Text;
using DummyClient;
using ServerCore;
using UnityEngine;

class PacketHandler
{
    public static void S_BroadcastEnterGameHandler(PacketSession session, IPacket packet)
    {
        S_BroadcastEnterGame pkt = packet as S_BroadcastEnterGame;
        ServerSession serverSession = session as ServerSession;

        Debug.Log($"[RECV] S_BroadcastEnterGame - 플레이어 {pkt.playerId} 입장, 위치: ({pkt.posX}, {pkt.posY}, {pkt.posZ})");
        PlayerManager.Instance.EnterGame(pkt);
    }

    public static void S_BroadcastLeaveGameHandler(PacketSession session, IPacket packet)
    {
        S_BroadcastLeaveGame pkt = packet as S_BroadcastLeaveGame;
        ServerSession serverSession = session as ServerSession;

        Debug.Log($"[RECV] S_BroadcastLeaveGame - 플레이어 {pkt.playerId} 퇴장");
        PlayerManager.Instance.LeaveGame(pkt);
    }

    public static void S_PlayerListHandler(PacketSession session, IPacket packet)
    {
        S_PlayerList pkt = packet as S_PlayerList;
        ServerSession serverSession = session as ServerSession;

        Debug.Log($"[RECV] S_PlayerList - 플레이어 목록 수신, 총 {pkt.players.Count}명");
        PlayerManager.Instance.Add(pkt);
    }

    public static void S_BroadcastMoveHandler(PacketSession session, IPacket packet)
    {
        S_BroadcastMove pkt = packet as S_BroadcastMove;
        ServerSession serverSession = session as ServerSession;

        Debug.Log($"[RECV] S_BroadcastMove - 플레이어 {pkt.playerId} 이동, 위치: ({pkt.posX}, {pkt.posY}, {pkt.posZ})");
        PlayerManager.Instance.Move(pkt);
    }
}
