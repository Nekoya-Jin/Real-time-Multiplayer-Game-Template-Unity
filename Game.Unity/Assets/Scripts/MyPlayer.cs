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
    [SerializeField] float _moveSpeed = 6f;
    [SerializeField] float _packetInterval = 0.25f;

    NetworkManager _networkManager = null!;
    CancellationTokenSource? _movementCts;

    void Awake()
    {
        Debug.Log("[MyPlayer] Initializing local player");
        EnsureNetworkManager();
    }

    void OnEnable()
    {
        Debug.Log("[MyPlayer] Starting movement loop");
        _movementCts = new CancellationTokenSource();
        MovementLoopAsync(_movementCts.Token).Forget();
    }

    void OnDisable()
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

    void Update()
    {
        HandleMovementInput();
    }

    void EnsureNetworkManager()
    {
        if (_networkManager != null)
            return;

        _networkManager = NetworkManager.TryGetOrCreate();
        _networkManager.RegisterLocalPlayer(this);
    }

    void HandleMovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        var direction = new UnityVector3(h, 0f, v);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        transform.position += direction.normalized * (_moveSpeed * Time.deltaTime);
    }

    async UniTaskVoid MovementLoopAsync(CancellationToken token)
    {
        await UniTask.SwitchToMainThread();
        Debug.Log($"[MyPlayer] Movement loop started (interval: {_packetInterval}s)");

        while (!token.IsCancellationRequested)
        {
            if (_networkManager == null)
                _networkManager = NetworkManager.TryGetOrCreate();

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
                await UniTask.Delay(TimeSpan.FromSeconds(_packetInterval), cancellationToken: token);
            } catch (OperationCanceledException)
            {
                Debug.Log("[MyPlayer] Movement loop cancelled");
                break;
            }
        }
    }

    UnityVector3 PickRandomTarget()
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
