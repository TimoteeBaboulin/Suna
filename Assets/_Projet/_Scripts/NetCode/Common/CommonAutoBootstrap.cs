using UnityEngine;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Entities;
using Unity.Services.Multiplayer;
using GameNetwork;
using GameNetwork.Utils;
using System.Net.Sockets;
using System.Net;


public class CommonAutoConnect : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer
            || RequestedPlayType == PlayType.ClientAndServer)
        {
            AutoConnectPort = 0;
            return false;
        }
        else if (Application.platform == RuntimePlatform.WindowsServer)
        {
            AutoConnectPort = ClientTransportHelper.GetAvailablePort();
            ClientTransportHelper.CurrentPort = AutoConnectPort;
            ClientTransportHelper.ServerWorld = CreateServerWorld("ServerWorld");
        }
        return true;
    }
}




