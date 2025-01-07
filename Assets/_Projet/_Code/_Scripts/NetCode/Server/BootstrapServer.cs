using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
public class BootstrapServer : ClientServerBootstrap
{
#if UNITY_SERVER
    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 7979;
        CreateServerWorld("Server");
        DefaultListenAddress = NetworkEndpoint.AnyIpv4.WithPort(AutoConnectPort);
        /*World serverWorld = CreateServerWorld("Server");*/
        //NetworkEndpoint serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(7979);
        //{
        //    using EntityQuery networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
        //    networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
        //}

        return true;
    }
#endif
}


