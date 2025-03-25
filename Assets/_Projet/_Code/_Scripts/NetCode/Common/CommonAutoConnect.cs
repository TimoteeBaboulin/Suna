using UnityEngine;
using Unity.NetCode;
using Unity.Networking.Transport;
public class CommonAutoConnect : ClientServerBootstrap
{
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
            CreateServerWorld("ServerWorld");
            DefaultListenAddress = NetworkEndpoint.AnyIpv4.WithPort(AutoConnectPort);

            return true;
        }
        return true;
    }
}


