using System.Threading;
using Cysharp.Threading.Tasks;
using RealTimeGame.Shared.Contracts;
using UnityEngine;
using SharedVector3 = RealTimeGame.Shared.Contracts.Vector3;

#nullable enable

/// <summary>
/// 네트워크 관리자 - 세션과 플레이어 레지스트리를 조율
/// </summary>
public sealed class NetworkManager : MonoBehaviour, IGameHubReceiver
{
    [Header("Connection")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 7070;
    [SerializeField, Min(0.1f)] private float reconnectDelaySeconds = 2f;

    [Header("Game")]
    [SerializeField] private GameObject playerPrefab = null!;

    public static NetworkManager? Instance { get; private set; }
    public bool IsConnected => _session?.IsConnected ?? false;

    private NetworkSession? _session;
    private PlayerRegistry? _playerRegistry;
    private CancellationTokenSource? _lifetimeCts;

    #region Unity Lifecycle

    private void Awake()
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

        _session = new NetworkSession(host, port, reconnectDelaySeconds, this);
        _playerRegistry = new PlayerRegistry(playerPrefab);
        _lifetimeCts = new CancellationTokenSource();

        Debug.Log("[NetworkManager] Starting auto-connect...");
        AutoConnectAsync(_lifetimeCts.Token).Forget();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        _lifetimeCts?.Cancel();
        _lifetimeCts?.Dispose();
        _lifetimeCts = null;

        _session?.Dispose();
        _session = null;
    }

    private void OnApplicationQuit()
    {
        _lifetimeCts?.Cancel();
        _session?.DisconnectAsync().Forget();
    }

    #endregion

    #region Public API

    public static NetworkManager TryGetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var existing = FindAnyObjectByType<NetworkManager>();
        if (existing != null)
        {
            return existing;
        }

        var go = new GameObject("NetworkManager");
        return go.AddComponent<NetworkManager>();
    }

    public void RegisterLocalPlayer(MyPlayer player)
    {
        _playerRegistry?.RegisterLocalPlayer(player);
    }

    public async UniTask EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_session != null)
        {
            await _session.EnsureConnectedAsync(cancellationToken);
        }
    }

    public void SendMove(SharedVector3 position)
    {
        _session?.SendMove(position);
    }

    public void SendLeave()
    {
        _session?.SendLeave();
    }

    #endregion

    #region Private Methods

    private async UniTaskVoid AutoConnectAsync(CancellationToken cancellationToken)
    {
        if (_session == null || _playerRegistry == null)
        {
            return;
        }

        try
        {
            var response = await _session.ConnectAsync(cancellationToken);
            _playerRegistry.ApplyJoinResponse(response);
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[NetworkManager] Auto-connect cancelled");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkManager] Auto-connect failed: {ex.Message}");
        }
    }

    #endregion



    #region IGameHubReceiver

    public void OnPlayerJoined(PlayerInfo player)
    {
        Debug.Log($"[NetworkManager] ▶ Event: Player joined - ID: {player.PlayerId}, Name: {player.Name}");
        UniTask.Post(() => _playerRegistry?.SpawnOrUpdate(player));
    }

    public void OnPlayerLeft(int playerId)
    {
        Debug.Log($"[NetworkManager] ◀ Event: Player left - ID: {playerId}");
        UniTask.Post(() => _playerRegistry?.RemovePlayer(playerId));
    }

    public void OnPlayerMoved(PlayerInfo player)
    {
        Debug.Log($"[NetworkManager] ↔ Event: Player moved - ID: {player.PlayerId}, Pos: ({player.Position.X:F1}, {player.Position.Y:F1}, {player.Position.Z:F1})");
        UniTask.Post(() => _playerRegistry?.SpawnOrUpdate(player));
    }

    #endregion
}
