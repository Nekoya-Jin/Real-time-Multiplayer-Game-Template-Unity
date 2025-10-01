using System.Collections.Generic;
using RealTimeGame.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace DummyClient
{
    internal sealed class ClientReceiver : IGameHubReceiver
    {
        private readonly ILogger<ClientReceiver> _logger;
        private readonly Dictionary<int, PlayerInfo> _players = new();
        private int _selfId;

        public ClientReceiver(ILogger<ClientReceiver> logger)
        {
            _logger = logger;
        }

        public void SetSelf(int playerId)
        {
            _selfId = playerId;
        }

        public void OnPlayerJoined(PlayerInfo player)
        {
            _players[player.PlayerId] = player;
            _logger.LogInformation("Player joined PlayerId={PlayerId} Name={Name}", player.PlayerId, player.Name);
        }

        public void OnPlayerLeft(int playerId)
        {
            _players.Remove(playerId);
            _logger.LogInformation("Player left PlayerId={PlayerId}", playerId);
        }

        public void OnPlayerMoved(PlayerInfo player)
        {
            _players[player.PlayerId] = player;
            if (player.PlayerId == _selfId)
            {
                _logger.LogDebug("Move ack PlayerId={PlayerId} Pos=({X:F1},{Y:F1},{Z:F1})",
                    player.PlayerId, player.Position.X, player.Position.Y, player.Position.Z);
            }
            else
            {
                _logger.LogInformation("Player moved PlayerId={PlayerId} Pos=({X:F1},{Y:F1},{Z:F1})",
                    player.PlayerId, player.Position.X, player.Position.Y, player.Position.Z);
            }
        }
    }
}
