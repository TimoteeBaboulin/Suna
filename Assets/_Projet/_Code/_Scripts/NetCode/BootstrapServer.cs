using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine.LightTransport;

public class BootstrapServer : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        World serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        NetworkEndpoint serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(7979);
        {
            using EntityQuery networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
        }
        return true;
    }
}


