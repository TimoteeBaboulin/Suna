using UnityEngine;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Entities;
using Unity.Services.Multiplayer;
using GameNetwork;
public class CommonAutoConnect : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        // The game is using the GameBoostrap to start.
        if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer 
            || RequestedPlayType == PlayType.ClientAndServer)
        {
            AutoConnectPort = 0;
            return false;
        }
        else if (Application.platform == RuntimePlatform.WindowsServer)
        {
            AutoConnectPort = 7979;
            CreateServerWorld("ServerWorld");
            //DefaultListenAddress = NetworkEndpoint.AnyIpv4.WithPort(AutoConnectPort);

            return true;
        }
        return true;
    }
}


