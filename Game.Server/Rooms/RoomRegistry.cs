using System.Collections.Concurrent;

namespace Server.Rooms
{
    public class RoomRegistry
    {
        private readonly ConcurrentDictionary<string, RoomState> _rooms = new();

        public RoomState GetOrAdd(string roomName)
            => _rooms.GetOrAdd(roomName, static name => new RoomState(name));
    }
}
