using System;
using System.Linq;
using System.Threading.Tasks;
using RealTimeGame.Shared.Contracts;
using MagicOnion.Server.Hubs;
using Microsoft.Extensions.Logging;
using Server.Rooms;

namespace Server.Hubs
{
    public class GameHub : StreamingHubBase<IGameHub, IGameHubReceiver>, IGameHub
    {
        private const string DefaultRoomName = "default";
        private readonly RoomRegistry _roomRegistry;
        private readonly ILogger<GameHub> _logger;

        private RoomState? _room;
        private IGroup<IGameHubReceiver>? _group;
        private PlayerState? _self;

        public GameHub(RoomRegistry roomRegistry, ILogger<GameHub> logger)
        {
            _roomRegistry = roomRegistry;
            _logger = logger;
        }

        public async Task<JoinResponse> JoinAsync(JoinRequest request)
        {
            var roomName = DefaultRoomName;
            _room = _roomRegistry.GetOrAdd(roomName);
            _group = await Group.AddAsync(roomName);

            string requestedName = string.IsNullOrWhiteSpace(request.Name)
                ? $"Player-{Guid.NewGuid().ToString("N").Substring(0, 6)}"
                : request.Name;

            _self = _room.AddPlayer(requestedName, request.InitialPosition);

            var selfInfo = ToPlayerInfo(_self);
            _logger.LogInformation("Player joined: {PlayerId} ({PlayerName})", selfInfo.PlayerId, selfInfo.Name);

            _group.Except(new[] { ConnectionId }).OnPlayerJoined(selfInfo);

            var players = _room.Snapshot().Select(ToPlayerInfo).ToArray();

            return new JoinResponse
            {
                Self = selfInfo,
                Players = players
            };
        }

        public Task MoveAsync(Vector3 position)
        {
            if (_room == null || _group == null || _self == null)
            {
                _logger.LogWarning("MoveAsync called before JoinAsync finished.");
                return Task.CompletedTask;
            }

            _self.UpdatePosition(position);
            _group.Except(new[] { ConnectionId }).OnPlayerMoved(ToPlayerInfo(_self));
            return Task.CompletedTask;
        }

        public async Task LeaveAsync()
        {
            if (_room == null || _group == null || _self == null)
            {
                return;
            }

            if (_room.TryRemove(_self.PlayerId, out _))
            {
                _group.Except(new[] { ConnectionId }).OnPlayerLeft(_self.PlayerId);
                _logger.LogInformation("Player left: {PlayerId}", _self.PlayerId);
            }

            await _group.RemoveAsync(this.Context);

            _room = null;
            _group = null;
            _self = null;
        }

        protected override ValueTask OnDisconnected()
        {
            return new ValueTask(LeaveAsync());
        }

        private static PlayerInfo ToPlayerInfo(PlayerState state)
        {
            return new PlayerInfo
            {
                PlayerId = state.PlayerId,
                Name = state.Name,
                Position = state.Position
            };
        }
    }
}
