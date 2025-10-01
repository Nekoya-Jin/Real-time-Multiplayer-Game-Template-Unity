using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using RealTimeGame.Shared.Contracts;

namespace Server.Rooms
{
    public class RoomState
    {
        private readonly ConcurrentDictionary<int, PlayerState> _players = new();
        private int _nextPlayerId;

        public RoomState(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public PlayerState AddPlayer(string name, Vector3 initialPosition)
        {
            int id = Interlocked.Increment(ref _nextPlayerId);
            var player = new PlayerState(id, name, initialPosition);
            _players[id] = player;
            return player;
        }

        public bool TryRemove(int playerId, [MaybeNullWhen(false)] out PlayerState player)
        {
            return _players.TryRemove(playerId, out player);
        }

        public bool TryGet(int playerId, [MaybeNullWhen(false)] out PlayerState player)
        {
            return _players.TryGetValue(playerId, out player);
        }

        public PlayerState[] Snapshot()
        {
            return _players.Values.ToArray();
        }
    }
}
