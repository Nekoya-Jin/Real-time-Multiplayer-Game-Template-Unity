using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Serialization.MemoryPack;
using RealTimeGame.Shared.Contracts;
using UnityEngine;
using SharedVector3 = RealTimeGame.Shared.Contracts.Vector3;

#nullable enable

/// <summary>
/// 네트워크 세션 관리 - 연결, 재연결, Hub 통신 담당
/// </summary>
public sealed class NetworkSession : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly float _reconnectDelaySeconds;
    private readonly IGameHubReceiver _receiver;

    private IGameHub? _hub;
    private GrpcChannelx? _channel;
    private bool _isConnecting;
    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public IGameHub? Hub => _hub;

    public NetworkSession(string host, int port, float reconnectDelaySeconds, IGameHubReceiver receiver)
    {
        _host = host;
        _port = port;
        _reconnectDelaySeconds = reconnectDelaySeconds;
        _receiver = receiver;
    }

    /// <summary>
    /// 연결 보장 - 연결되지 않았으면 연결 시도, 실패 시 재시도
    /// </summary>
    public async UniTask EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_isConnected)
        {
            return;
        }

        if (_isConnecting)
        {
            Debug.Log("[NetworkSession] Already connecting, waiting...");
            while (_isConnecting && !_isConnected && !cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            return;
        }

        Debug.Log("[NetworkSession] Starting connection attempt...");
        _isConnecting = true;

        try
        {
            while (!_isConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("[NetworkSession] Connection cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkSession] Connection failed: {ex.Message}. Retrying in {_reconnectDelaySeconds}s...");
                    await DisconnectAsync();
                    await UniTask.Delay(TimeSpan.FromSeconds(_reconnectDelaySeconds), cancellationToken: cancellationToken);
                }
            }
        }
        finally
        {
            _isConnecting = false;
        }
    }

    /// <summary>
    /// 서버 연결 및 Join 요청
    /// </summary>
    public async UniTask<JoinResponse> ConnectAsync(CancellationToken cancellationToken)
    {
        await DisconnectAsync();

        var endpoint = $"http://{_host}:{_port}";
        Debug.Log($"[NetworkSession] Connecting to server: {endpoint}");

        _channel = GrpcChannelx.ForAddress(endpoint);
        var serializerProvider = MemoryPackMagicOnionSerializerProvider.Instance;

        try
        {
            Debug.Log("[NetworkSession] Establishing StreamingHub connection...");
            _hub = await StreamingHubClient.ConnectAsync<IGameHub, IGameHubReceiver>(
                _channel,
                _receiver,
                serializerProvider: serializerProvider,
                cancellationToken: cancellationToken);
            Debug.Log("[NetworkSession] StreamingHub connected successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSession] Hub connection failed: {ex.Message}");
            throw;
        }

        var joinRequest = new JoinRequest
        {
            Name = $"UnityClient_{UnityEngine.Random.Range(1000, 9999)}",
            InitialPosition = new SharedVector3(0, 0, 0)
        };

        try
        {
            Debug.Log($"[NetworkSession] Sending join request as '{joinRequest.Name}'...");
            var response = await _hub.JoinAsync(joinRequest);
            _isConnected = true;
            Debug.Log($"[NetworkSession] ✓ Connected successfully! Player ID: {response.Self?.PlayerId}");
            return response;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSession] Join request failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 서버 연결 해제
    /// </summary>
    public async UniTask DisconnectAsync()
    {
        if (_isConnected)
        {
            Debug.Log("[NetworkSession] Disconnecting from server...");
        }

        _isConnected = false;

        if (_hub != null)
        {
            try
            {
                await _hub.DisposeAsync();
            }
            catch { }
            finally
            {
                _hub = null;
            }
        }

        if (_channel != null)
        {
            try
            {
                _channel.Dispose();
            }
            catch { }
            finally
            {
                _channel = null;
            }
        }

        Debug.Log("[NetworkSession] Disconnected");
    }

    /// <summary>
    /// 플레이어 이동 전송
    /// </summary>
    public void SendMove(SharedVector3 position)
    {
        if (_hub != null)
        {
            Debug.Log($"[NetworkSession] Sending move to server: ({position.X:F1}, {position.Y:F1}, {position.Z:F1})");
            _hub.MoveAsync(position).AsUniTask().Forget();
        }
    }

    /// <summary>
    /// 게임 나가기 전송
    /// </summary>
    public void SendLeave()
    {
        if (_hub != null)
        {
            Debug.Log("[NetworkSession] Sending leave request to server");
            _hub.LeaveAsync().AsUniTask().Forget();
        }
    }

    public void Dispose()
    {
        DisconnectAsync().Forget();
    }
}
