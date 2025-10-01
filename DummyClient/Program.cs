using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RealTimeGame.Shared.Contracts;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Client;
using MagicOnion.Serialization.MemoryPack;
using Microsoft.Extensions.Logging;

namespace DummyClient
{
    internal static class Program
    {
        private const string DefaultServerAddress = "http://127.0.0.1:7070";

        private static async Task<int> Main(string[] args)
        {
            int clientCount = ParseClientCount(args);

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                loggerFactory.CreateLogger("Shutdown").LogInformation("Cancellation requested — shutting down clients...");
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            var tasks = Enumerable.Range(0, clientCount)
                .Select(index => RunClientAsync(index, loggerFactory, cts.Token))
                .ToArray();

            await Task.WhenAll(tasks);
            return 0;
        }

        private static int ParseClientCount(IReadOnlyList<string> args)
        {
            if (args.Count > 0 && int.TryParse(args[0], out int count) && count > 0)
                return count;
            return 5;
        }

        private static async Task RunClientAsync(int index, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger($"Client-{index}");
            var receiver = new ClientReceiver(loggerFactory.CreateLogger<ClientReceiver>());

            using var httpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            };

            using var channel = GrpcChannel.ForAddress(DefaultServerAddress, new GrpcChannelOptions
            {
                LoggerFactory = loggerFactory,
                HttpHandler = httpHandler
            });

            logger.LogInformation("Connecting to {Server}", DefaultServerAddress);

            var hubOptions = StreamingHubClientOptions.CreateWithDefault(
                    new Uri(DefaultServerAddress).Host,
                    callOptions: default,
                    serializerProvider: MemoryPackMagicOnionSerializerProvider.Instance,
                    logger: NullMagicOnionClientLogger.Instance)
                .WithClientHeartbeatInterval(TimeSpan.FromSeconds(5))
                .WithClientHeartbeatTimeout(TimeSpan.FromSeconds(15));

            var hub = await StreamingHubClient.ConnectAsync<IGameHub, IGameHubReceiver>(
                channel,
                receiver,
                hubOptions,
                StreamingHubClientFactoryProvider.Default,
                cancellationToken);

            var random = new Random(Guid.NewGuid().GetHashCode());
            var joinResponse = await hub.JoinAsync(new JoinRequest
            {
                Name = $"Bot-{index:000}",
                InitialPosition = new Vector3(
                    random.Next(-20, 21),
                    0f,
                    random.Next(-20, 21))
            });

            receiver.SetSelf(joinResponse.Self.PlayerId);
            logger.LogInformation("Joined as PlayerId={PlayerId}", joinResponse.Self.PlayerId);

            foreach (var player in joinResponse.Players)
            {
                receiver.OnPlayerJoined(player);
                logger.LogInformation("Existing player PlayerId={PlayerId} Name={Name} Pos=({X:F1},{Y:F1},{Z:F1})",
                    player.PlayerId, player.Name, player.Position.X, player.Position.Y, player.Position.Z);
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var nextPos = new Vector3(
                        random.Next(-50, 51),
                        0f,
                        random.Next(-50, 51));

                    await hub.MoveAsync(nextPos);
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                // ignore cancellation
            }
            finally
            {
                try
                {
                    await hub.LeaveAsync();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error during leave");
                }

                await hub.DisposeAsync();
                logger.LogInformation("Client {Index} disconnected", index);
            }
        }
    }
}
