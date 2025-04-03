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
    public string SessionCode { get; private set; } = "DDDDDD";
    private ClientTransportHelper serverConnectionSettings;

    static public ServerSessionFactory instance { get; private set; }
    private ServerSessionFactory() { }

    public static async Task<ClientTransportHelper> CreateServerSession(string ip, ushort port, bool isClientLocal)
    {
        instance = new ServerSessionFactory();
        try
        {
            SessionOptions options = new SessionOptions { MaxPlayers = ClientTransportHelper.MaxNbPlayers };
            ClientTransportHelper transportHelper = new ClientTransportHelper(ip, port, isClientLocal);
            ClientTransportHelper serverSession = await transportHelper.CreateServerSessionAsync(options);

            Debug.Log($"[SessionTransportHelper] Creating server session with options: MaxPlayers={options.MaxPlayers}");
            Debug.Log($"[SessionTransportHelper] IP: {ip}, Port: {port}, IsClientLocal: {isClientLocal}");
            Debug.Log($"[ServerSessionFactory] Created session with code: {serverSession.Session.Id}");
            return serverSession;

        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerSessionSettings] Error creating session: {ex}");
            return null;
        }
    }
}
