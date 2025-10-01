using System.Collections.Generic;
using RealTimeGame.Shared.Contracts;
using UnityEngine;
using UnityVector3 = UnityEngine.Vector3;

#nullable enable

/// <summary>
/// 플레이어 레지스트리 - 플레이어 생성, 업데이트, 제거 관리
/// </summary>
public sealed class PlayerRegistry
{
    private readonly GameObject _playerPrefab;
    private readonly Dictionary<int, Player> _players = new();
    private MyPlayer? _localPlayer;
    private int _selfPlayerId = -1;

    public int SelfPlayerId => _selfPlayerId;
    public MyPlayer? LocalPlayer => _localPlayer;
    public IReadOnlyDictionary<int, Player> Players => _players;

    public PlayerRegistry(GameObject playerPrefab)
    {
        _playerPrefab = playerPrefab;
    }

    /// <summary>
    /// 로컬 플레이어 등록
    /// </summary>
    public void RegisterLocalPlayer(MyPlayer player)
    {
        Debug.Log($"[PlayerRegistry] Registering local player: {player.name}");
        _localPlayer = player;
        if (_selfPlayerId != -1)
        {
            _players[_selfPlayerId] = player;
            player.OverridePlayerId(_selfPlayerId);
            Debug.Log($"[PlayerRegistry] Local player assigned ID: {_selfPlayerId}");
        }
    }

    /// <summary>
    /// Join 응답 처리 - 자신과 기존 플레이어들 생성
    /// </summary>
    public void ApplyJoinResponse(JoinResponse response)
    {
        if (response == null)
        {
            return;
        }

        _selfPlayerId = response.Self?.PlayerId ?? -1;
        Debug.Log($"[PlayerRegistry] Join response received. Self ID: {_selfPlayerId}");

        if (_localPlayer != null && response.Self != null)
        {
            _players[_selfPlayerId] = _localPlayer;
            _localPlayer.ApplyInfo(response.Self);
            Debug.Log($"[PlayerRegistry] Applied info to local player: {response.Self.Name}");
        }
        else
        {
            SpawnOrUpdate(response.Self);
        }

        Debug.Log($"[PlayerRegistry] Spawning {response.Players.Length} existing player(s)...");
        foreach (var player in response.Players)
        {
            if (player != null)
            {
                SpawnOrUpdate(player);
            }
        }
    }

    /// <summary>
    /// 플레이어 생성 또는 업데이트
    /// </summary>
    public void SpawnOrUpdate(PlayerInfo? info)
    {
        if (info == null)
        {
            return;
        }

        if (_players.TryGetValue(info.PlayerId, out var existing))
        {
            Debug.Log($"[PlayerRegistry] Updating player {info.PlayerId}: {info.Name}");
            existing.ApplyInfo(info);
            return;
        }

        Debug.Log($"[PlayerRegistry] Spawning new player {info.PlayerId}: {info.Name}");
        var go = Object.Instantiate(_playerPrefab, UnityVector3.zero, Quaternion.identity);

        Player target;
        if (_selfPlayerId != -1 && info.PlayerId == _selfPlayerId)
        {
            target = go.GetComponent<MyPlayer>() ?? go.AddComponent<MyPlayer>();
            _localPlayer = (MyPlayer)target;
            Debug.Log($"[PlayerRegistry] Spawned as LOCAL player");
        }
        else
        {
            target = go.GetComponent<Player>() ?? go.AddComponent<Player>();
            Debug.Log($"[PlayerRegistry] Spawned as REMOTE player");
        }

        target.ApplyInfo(info);
        _players[info.PlayerId] = target;
    }

    /// <summary>
    /// 플레이어 제거
    /// </summary>
    public void RemovePlayer(int playerId)
    {
        if (_players.TryGetValue(playerId, out var player) && player != null)
        {
            _players.Remove(playerId);
            if (player != _localPlayer)
            {
                Debug.Log($"[PlayerRegistry] Destroying player {playerId} object");
                Object.Destroy(player.gameObject);
            }
        }
    }

    /// <summary>
    /// 모든 원격 플레이어 제거 (연결 해제 시)
    /// </summary>
    public void ClearRemotePlayers()
    {
        int cleanedCount = 0;
        foreach (var kvp in _players)
        {
            if (kvp.Value != null && kvp.Value != _localPlayer)
            {
                Object.Destroy(kvp.Value.gameObject);
                cleanedCount++;
            }
        }

        if (cleanedCount > 0)
        {
            Debug.Log($"[PlayerRegistry] Cleaned up {cleanedCount} remote player(s)");
        }

        _players.Clear();
        if (_localPlayer != null && _selfPlayerId != -1)
        {
            _players[_selfPlayerId] = _localPlayer;
        }

        _selfPlayerId = -1;
    }
}
