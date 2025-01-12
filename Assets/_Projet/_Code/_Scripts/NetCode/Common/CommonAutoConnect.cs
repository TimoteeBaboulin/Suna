using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
public class CommonAutoConnect : ClientServerBootstrap
{
//#if UNITY_SERVER && !UNITY_EDITOR
//    public override bool Initialize(string defaultWorldName)
//    {
//        AutoConnectPort = 7979;
//        ConnectionManager.Instance.CreateServer();
//        //CreateServerWorld("Server");
//        //ConnectionManager.Instance.Server = CreateServerWorld("Server");
//        DefaultListenAddress = NetworkEndpoint.AnyIpv4.WithPort(AutoConnectPort);
//        //ConnectionManager.Instance.Role = ConnectionManager.RoleType.Server;
//        /*World serverWorld = CreateServerWorld("Server");*/
//        //NetworkEndpoint serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(7979);
//        //{
//        //    using EntityQuery networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
//        //    networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
//        //}

//        return true;
//    }
//#endif

//#if UNITY_CLIENT || UNITY_EDITOR
    public override bool Initialize(string defaultWorldName)
    {
        if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            AutoConnectPort = 0;
            return false;
        }
        else if (Application.platform == RuntimePlatform.WindowsServer)
        {
            AutoConnectPort = 7979;
            ConnectionManager.Instance.CreateServer();
            //CreateServerWorld("Server");
            //ConnectionManager.Instance.Server = CreateServerWorld("Server");
            DefaultListenAddress = NetworkEndpoint.AnyIpv4.WithPort(AutoConnectPort);
            //ConnectionManager.Instance.Role = ConnectionManager.RoleType.Server;
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
//#endif
}


