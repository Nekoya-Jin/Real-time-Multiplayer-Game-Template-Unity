using RealTimeGame.Shared.Contracts;

namespace Server.Rooms
{
    public class PlayerState
    {
        public PlayerState(int playerId, string name, Vector3 position)
        {
            PlayerId = playerId;
            Name = name;
            Position = position;
        }

        public int PlayerId { get; }
        public string Name { get; }
        public Vector3 Position { get; private set; }

        public void UpdatePosition(Vector3 position)
        {
            Position = position;
        }
    }
}
