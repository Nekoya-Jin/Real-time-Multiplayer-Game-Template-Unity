using RealTimeGame.Shared.Contracts;
using UnityEngine;
using SharedVector3 = RealTimeGame.Shared.Contracts.Vector3;
using UnityVector3 = UnityEngine.Vector3;

/// <summary>
/// 기본 플레이어 클래스 - 원격 플레이어에 사용됩니다
/// </summary>
public class Player : MonoBehaviour
{
    public int PlayerId { get; protected set; }
    public string PlayerName { get; protected set; } = string.Empty;

    /// <summary>
    /// 서버에서 전달된 플레이어 정보를 현재 오브젝트에 반영합니다
    /// </summary>
    public virtual void ApplyInfo(PlayerInfo info)
    {
        if (info == null)
        {
            return;
        }

        PlayerId = info.PlayerId;
        PlayerName = info.Name;
        transform.position = ToUnityVector(info.Position);
        gameObject.name = string.IsNullOrEmpty(PlayerName)
            ? $"Player_{PlayerId}"
            : $"Player_{PlayerId}_{PlayerName}";

        Debug.Log($"[Player] Applied info - ID: {PlayerId}, Name: {PlayerName}, Pos: {transform.position}");
    }

    internal void OverridePlayerId(int playerId)
    {
        PlayerId = playerId;
    }

    protected static UnityVector3 ToUnityVector(SharedVector3 value)
    {
        return new UnityVector3(value.X, value.Y, value.Z);
    }

    protected static SharedVector3 ToSharedVector(UnityVector3 value)
    {
        return new SharedVector3(value.x, value.y, value.z);
    }
}
