using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
public class CommonAutoConnect : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        if (ConnectionManager.Instance.Role == ConnectionManager.RoleType.ServerClient || ConnectionManager.Instance.Role == ConnectionManager.RoleType.Client)
        {
            AutoConnectPort = 0;
            return false;
        }
        else if (ConnectionManager.Instance.Role == ConnectionManager.RoleType.Server)
        {
            AutoConnectPort = 7979;
            ConnectionManager.Instance.CreateServer();
            DefaultListenAddress = NetworkEndpoint.AnyIpv4.WithPort(AutoConnectPort);
            /*World serverWorld = CreateServerWorld("Server");*/
            //NetworkEndpoint serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(7979);
            //{
            //    using EntityQuery networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            //    networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
            //}

            return true;
        }
        return true;
    }
}


