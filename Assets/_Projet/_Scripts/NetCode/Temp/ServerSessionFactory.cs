using GameNetwork.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using UnityEngine;
using static Unity.NetCode.ClientServerBootstrap;

public class ServerSessionFactory
{
    private string SessionName ;
    private ClientTransportHelper serverConnectionSettings;

    static public ServerSessionFactory instance { get; private set; }
    private ServerSessionFactory() { }

    public static async Task<ClientTransportHelper> CreateServerSession(string ip, ushort port, bool isClientLocal)
    {
        instance = new ServerSessionFactory();
        instance.SessionName = RequestedPlayType == PlayType.ClientAndServer ? "ClientServer" : "Server";
        try
        {
            SessionOptions options = new SessionOptions { MaxPlayers = ClientTransportHelper.MaxNbOfPlayers , Name = instance.SessionName };
            ClientTransportHelper transportHelper = new ClientTransportHelper();
            ClientTransportHelper serverSession = await transportHelper.CreateServerSessionAsync(options);

            Debug.Log($"[SessionTransportHelper] Creating server session with options: MaxPlayers={options.MaxPlayers}");
            Debug.Log($"[SessionTransportHelper] IP: {ip}, Port: {port}, IsClientLocal: {isClientLocal}");
            Debug.Log($"[ServerSessionFactory] Created session with code: {serverSession.Session.Id}");
            Debug.Log($"[ServerSessionFactory] Created session with name: {serverSession.Session.Name}");
            return serverSession;

        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerSessionSettings] Error creating session: {ex}");
            return null;
        }
    }
}
