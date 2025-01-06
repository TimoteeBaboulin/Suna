using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

public partial struct BootstrapServer : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        World serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");

        NetworkEndpoint serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(7979);
        {
            using EntityQuery networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
        }

        Debug.Log("Server Start");
    }
}
