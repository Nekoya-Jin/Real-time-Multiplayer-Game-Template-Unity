using MagicOnion.Client;
using MagicOnion.Unity;
using Grpc.Net.Client;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// MagicOnion 클라이언트 초기화 설정
    /// </summary>
    [MagicOnionClientGeneration(
        typeof(RealTimeGame.Shared.Contracts.IGameHub),
        Serializer = MagicOnionClientGenerationAttribute.GenerateSerializerType.MemoryPack
    )]
    partial class MagicOnionClientInitializer { }

    /// <summary>
    /// 런타임 초기화 설정
    /// </summary>
    class InitialSettings
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void OnRuntimeInitialize()
        {
            Debug.Log("[InitialSettings] Initializing gRPC channel provider...");

            GrpcChannelProviderHost.Initialize(
                new GrpcNetClientGrpcChannelProvider(() => new GrpcChannelOptions()
                {
                    HttpHandler = new Cysharp.Net.Http.YetAnotherHttpHandler()
                    {
                        Http2Only = true,
                    }
                }));

            Debug.Log("[InitialSettings] gRPC channel provider initialized successfully");
        }
    }
}
