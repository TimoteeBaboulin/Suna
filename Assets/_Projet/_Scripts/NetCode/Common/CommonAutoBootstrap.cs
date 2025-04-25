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
            //Debug : 59692
            //Thomas : 59557
            AutoConnectPort = 59692; //Votre port ici
            ClientTransportHelper.ServerWorld = CreateServerWorld("ServerWorld");

            return true;
        }
        return true;
    }

    public ushort GetAvailablePort()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        ushort port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}


