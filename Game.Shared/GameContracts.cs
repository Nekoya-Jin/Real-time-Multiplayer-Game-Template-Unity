using System.Threading.Tasks;
using MagicOnion;
using MemoryPack;

namespace RealTimeGame.Shared.Contracts
{
    [MemoryPackable]
    public partial struct Vector3
    {
        [MemoryPackOrder(0)]
        public float X { get; set; }

        [MemoryPackOrder(1)]
        public float Y { get; set; }

        [MemoryPackOrder(2)]
        public float Z { get; set; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    [MemoryPackable]
    public partial class PlayerInfo
    {
        [MemoryPackOrder(0)]
        public int PlayerId { get; set; }

        [MemoryPackOrder(1)]
        public string Name { get; set; } = string.Empty;

        [MemoryPackOrder(2)]
        public Vector3 Position { get; set; }
    }

    [MemoryPackable]
    public partial class JoinRequest
    {
        [MemoryPackOrder(0)]
        public string Name { get; set; } = string.Empty;

        [MemoryPackOrder(1)]
        public Vector3 InitialPosition { get; set; }
    }

    [MemoryPackable]
    public partial class JoinResponse
    {
        [MemoryPackOrder(0)]
        public PlayerInfo Self { get; set; } = new PlayerInfo();

        [MemoryPackOrder(1)]
        public PlayerInfo[] Players { get; set; } = System.Array.Empty<PlayerInfo>();
    }

    public interface IGameHubReceiver
    {
        void OnPlayerJoined(PlayerInfo player);
        void OnPlayerLeft(int playerId);
        void OnPlayerMoved(PlayerInfo player);
    }

    public interface IGameHub : IStreamingHub<IGameHub, IGameHubReceiver>
    {
        Task<JoinResponse> JoinAsync(JoinRequest request);
        Task MoveAsync(Vector3 position);
        Task LeaveAsync();
    }
}
