using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using RealTimeGame.Shared.Contracts;
using UnityEngine;
using UnityVector3 = UnityEngine.Vector3;

#nullable enable

/// <summary>
/// 로컬 플레이어 클래스 - 입력 처리 및 서버와의 동기화를 담당합니다
/// </summary>
public class MyPlayer : Player
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float packetInterval = 0.25f;

    private NetworkManager _networkManager = null!;
    private CancellationTokenSource? _movementCts;

    private void Awake()
    {
        Debug.Log("[MyPlayer] Initializing local player");
        EnsureNetworkManager();
    }

    private void OnEnable()
    {
        Debug.Log("[MyPlayer] Starting movement loop");
        _movementCts = new CancellationTokenSource();
        MovementLoopAsync(_movementCts.Token).Forget();
    }

    private void OnDisable()
    {
        if (_networkManager != null && _networkManager.IsConnected)
        {
            Debug.Log("[MyPlayer] Leaving game...");
            _networkManager.SendLeave();
        }

        _movementCts?.Cancel();
        _movementCts?.Dispose();
        _movementCts = null;
        Debug.Log("[MyPlayer] Stopped movement loop");
    }

    private void Update()
    {
        HandleMovementInput();
    }

    private void EnsureNetworkManager()
    {
        if (_networkManager != null)
        {
            return;
        }

        _networkManager = NetworkManager.TryGetOrCreate();
        _networkManager.RegisterLocalPlayer(this);
    }

    private void HandleMovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        var direction = new UnityVector3(h, 0f, v);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        transform.position += direction.normalized * (moveSpeed * Time.deltaTime);
    }

    private async UniTaskVoid MovementLoopAsync(CancellationToken token)
    {
        await UniTask.SwitchToMainThread();
        Debug.Log($"[MyPlayer] Movement loop started (interval: {packetInterval}s)");

        while (!token.IsCancellationRequested)
        {
            if (_networkManager == null)
            {
                _networkManager = NetworkManager.TryGetOrCreate();
            }

            if (_networkManager != null)
            {
                await _networkManager.EnsureConnectedAsync(token);

                UnityVector3 target = PickRandomTarget();
                transform.position = target;
                Debug.Log($"[MyPlayer] Moving to random position: ({target.x:F1}, {target.y:F1}, {target.z:F1})");

                var position = ToSharedVector(target);
                _networkManager.SendMove(position);
            }

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(packetInterval), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[MyPlayer] Movement loop cancelled");
                break;
            }
        }
    }

    private UnityVector3 PickRandomTarget()
    {
        float x = UnityEngine.Random.Range(-50f, 50f);
        float z = UnityEngine.Random.Range(-50f, 50f);
        return new UnityVector3(x, 0f, z);
    }

    public override void ApplyInfo(PlayerInfo info)
    {
        base.ApplyInfo(info);
    }
}
