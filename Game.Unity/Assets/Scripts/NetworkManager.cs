using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Serialization.MemoryPack;
using RealTimeGame.Shared.Contracts;
using UnityEngine;
using SharedVector3 = RealTimeGame.Shared.Contracts.Vector3;
using UnityVector3 = UnityEngine.Vector3;

#nullable enable

/// <summary>
/// MagicOnion을 사용한 멀티플레이어 네트워크 관리자
/// </summary>
public sealed class NetworkManager : MonoBehaviour, IGameHubReceiver
{
    [Header("Connection")]
    [SerializeField] string _host = "127.0.0.1";
    [SerializeField] int _port = 7070;
    [SerializeField, Min(0.1f)] float _reconnectDelaySeconds = 2f;

    [Header("Game")]
    [SerializeField] GameObject _playerPrefab = null!;

    public static NetworkManager? Instance { get; private set; }
    public bool IsConnected => _isConnected;

    readonly Dictionary<int, Player> _players = new();
    MyPlayer? _localPlayer;
    int _selfPlayerId = -1;

    IGameHub? _hub;
    GrpcChannelx? _channel;
    CancellationTokenSource? _lifetimeCts;
    bool _isConnecting;
    bool _isConnected;

    #region Unity Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[NetworkManager] Another instance already exists. Destroying this one.");
            Destroy(gameObject);
            return;
        }

        Debug.Log("[NetworkManager] Initializing singleton instance");
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _lifetimeCts = new CancellationTokenSource();

        Debug.Log("[NetworkManager] Starting auto-connect...");
        ConnectInternalAsync(_lifetimeCts.Token).Forget();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        _lifetimeCts?.Cancel();
        _lifetimeCts?.Dispose();
        _lifetimeCts = null;
    }

    void OnApplicationQuit()
    {
        _lifetimeCts?.Cancel();
        DisconnectClientAsync().Forget();
    }

    #endregion

    #region Public API

    public static NetworkManager TryGetOrCreate()
    {
        if (Instance != null)
            return Instance;

        var existing = FindAnyObjectByType<NetworkManager>();
        if (existing != null)
            return existing;

        var go = new GameObject("NetworkManager");
        return go.AddComponent<NetworkManager>();
    }

    public void RegisterLocalPlayer(MyPlayer player)
    {
        Debug.Log($"[NetworkManager] Registering local player: {player.name}");
        _localPlayer = player;
        if (_selfPlayerId != -1)
        {
            _players[_selfPlayerId] = player;
            player.OverridePlayerId(_selfPlayerId);
            Debug.Log($"[NetworkManager] Local player assigned ID: {_selfPlayerId}");
        }
    }

    public async UniTask EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_isConnected)
            return;

        if (_isConnecting)
        {
            Debug.Log("[NetworkManager] Already connecting, waiting...");
            while (_isConnecting && !_isConnected && !cancellationToken.IsCancellationRequested)
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            return;
        }

        Debug.Log("[NetworkManager] Starting connection attempt...");
        _isConnecting = true;

        try
        {
            while (!_isConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectInternalAsync(cancellationToken);
                } catch (OperationCanceledException)
                {
                    Debug.Log("[NetworkManager] Connection cancelled");
                    throw;
                } catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkManager] Connection failed: {ex.Message}. Retrying in {_reconnectDelaySeconds}s...");
                    await DisconnectClientAsync();
                    await UniTask.Delay(TimeSpan.FromSeconds(_reconnectDelaySeconds), cancellationToken: cancellationToken);
                }
            }
        } finally
        {
            _isConnecting = false;
        }
    }

    public void SendMove(SharedVector3 position)
    {
        if (_hub != null)
        {
            Debug.Log($"[NetworkManager] Sending move to server: ({position.X:F1}, {position.Y:F1}, {position.Z:F1})");
            _hub.MoveAsync(position).AsUniTask().Forget();
        }
    }

    public void SendLeave()
    {
        if (_hub != null)
        {
            Debug.Log("[NetworkManager] Sending leave request to server");
            _hub.LeaveAsync().AsUniTask().Forget();
        }
    }

    #endregion

    #region Connection

    async UniTask ConnectInternalAsync(CancellationToken cancellationToken)
    {
        await DisconnectClientAsync();

        var endpoint = $"http://{_host}:{_port}";
        Debug.Log($"[NetworkManager] Connecting to server: {endpoint}");

        _channel = GrpcChannelx.ForAddress(endpoint);
        var serializerProvider = MemoryPackMagicOnionSerializerProvider.Instance;

        try
        {
            Debug.Log("[NetworkManager] Establishing StreamingHub connection...");
            _hub = await StreamingHubClient.ConnectAsync<IGameHub, IGameHubReceiver>(
                _channel,
                this,
                serializerProvider: serializerProvider,
                cancellationToken: cancellationToken);
            Debug.Log("[NetworkManager] StreamingHub connected successfully");
        } catch (Exception ex)
        {
            Debug.LogError($"[NetworkManager] Hub connection failed: {ex.Message}");
            throw;
        }

        var joinRequest = new JoinRequest
        {
            Name = $"UnityClient_{UnityEngine.Random.Range(1000, 9999)}",
            InitialPosition = new SharedVector3(0, 0, 0)
        };

        try
        {
            Debug.Log($"[NetworkManager] Sending join request as '{joinRequest.Name}'...");
            var response = await _hub.JoinAsync(joinRequest);
            ApplyJoinResponse(response);
            _isConnected = true;
            Debug.Log($"[NetworkManager] ✓ Connected successfully! Player ID: {_selfPlayerId}");
        } catch (Exception ex)
        {
            Debug.LogError($"[NetworkManager] Join request failed: {ex.Message}");
            throw;
        }
    }

    async UniTask DisconnectClientAsync()
    {
        if (_isConnected)
            Debug.Log("[NetworkManager] Disconnecting from server...");

        _isConnected = false;

        if (_hub != null)
        {
            try { await _hub.DisposeAsync(); } catch { } finally { _hub = null; }
        }

        if (_channel != null)
        {
            try { _channel.Dispose(); } catch { } finally { _channel = null; }
        }

        int cleanedCount = 0;
        foreach (var kvp in _players)
        {
            if (kvp.Value != null && kvp.Value != _localPlayer)
            {
                Destroy(kvp.Value.gameObject);
                cleanedCount++;
            }
        }

        if (cleanedCount > 0)
            Debug.Log($"[NetworkManager] Cleaned up {cleanedCount} remote player(s)");

        _players.Clear();
        if (_localPlayer != null && _selfPlayerId != -1)
            _players[_selfPlayerId] = _localPlayer;

        _selfPlayerId = -1;
        Debug.Log("[NetworkManager] Disconnected");
    }

    #endregion

    #region Player Management

    void ApplyJoinResponse(JoinResponse response)
    {
        if (response == null)
            return;

        _selfPlayerId = response.Self?.PlayerId ?? -1;
        Debug.Log($"[NetworkManager] Join response received. Self ID: {_selfPlayerId}");

        if (_localPlayer != null && response.Self != null)
        {
            _players[_selfPlayerId] = _localPlayer;
            _localPlayer.ApplyInfo(response.Self);
            Debug.Log($"[NetworkManager] Applied info to local player: {response.Self.Name}");
        } else
        {
            SpawnOrUpdate(response.Self);
        }

        Debug.Log($"[NetworkManager] Spawning {response.Players.Length} existing player(s)...");
        foreach (var player in response.Players)
        {
            if (player != null)
                SpawnOrUpdate(player);
        }
    }

    void SpawnOrUpdate(PlayerInfo? info)
    {
        if (info == null)
            return;

        if (_players.TryGetValue(info.PlayerId, out var existing))
        {
            Debug.Log($"[NetworkManager] Updating player {info.PlayerId}: {info.Name}");
            existing.ApplyInfo(info);
            return;
        }

        Debug.Log($"[NetworkManager] Spawning new player {info.PlayerId}: {info.Name}");
        var go = Instantiate(_playerPrefab, UnityVector3.zero, Quaternion.identity);

        Player target;
        if (_selfPlayerId != -1 && info.PlayerId == _selfPlayerId)
        {
            target = go.GetComponent<MyPlayer>() ?? go.AddComponent<MyPlayer>();
            _localPlayer = (MyPlayer)target;
            Debug.Log($"[NetworkManager] Spawned as LOCAL player");
        } else
        {
            target = go.GetComponent<Player>() ?? go.AddComponent<Player>();
            Debug.Log($"[NetworkManager] Spawned as REMOTE player");
        }

        target.ApplyInfo(info);
        _players[info.PlayerId] = target;
    }

    #endregion

    #region IGameHubReceiver

    public void OnPlayerJoined(PlayerInfo player)
    {
        Debug.Log($"[NetworkManager] ▶ Event: Player joined - ID: {player.PlayerId}, Name: {player.Name}");
        UniTask.Post(() => SpawnOrUpdate(player));
    }

    public void OnPlayerLeft(int playerId)
    {
        Debug.Log($"[NetworkManager] ◀ Event: Player left - ID: {playerId}");
        UniTask.Post(() =>
        {
            if (_players.TryGetValue(playerId, out var player) && player != null)
            {
                _players.Remove(playerId);
                if (player != _localPlayer)
                {
                    Debug.Log($"[NetworkManager] Destroying player {playerId} object");
                    Destroy(player.gameObject);
                }
            }
        });
    }

    public void OnPlayerMoved(PlayerInfo player)
    {
        Debug.Log($"[NetworkManager] ↔ Event: Player moved - ID: {player.PlayerId}, Pos: ({player.Position.X:F1}, {player.Position.Y:F1}, {player.Position.Z:F1})");
        UniTask.Post(() => SpawnOrUpdate(player));
    }

    #endregion
}
