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
    public int MaxPlayers = 2;
    public string SessionCode { get; private set; } = "DDDDDD";
    private SessionTransportHelper serverConnectionSettings;

    static public ServerSessionFactory instance { get; private set; }
    private ServerSessionFactory() { }

    public static async Task<SessionTransportHelper> CreateServerSession(string ip, ushort port, bool isClientLocal)
    {
        instance = new ServerSessionFactory();
        // Only run session creation on a dedicated server.
        Debug.Log($"[instance] Error creating session: {instance}");
        Debug.Log($"[Application.platform == RuntimePlatform.WindowsServer] {Application.platform == RuntimePlatform.WindowsServer}");
        try
        {
            SessionOptions options = new SessionOptions { MaxPlayers = instance.MaxPlayers };
            SessionTransportHelper transportHelper = new SessionTransportHelper(ip, port, isClientLocal);
            SessionTransportHelper serverSession = await transportHelper.CreateServerSessionAsync(options);

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

    public static async Task<IMultiplaySessionManager> StartMultiplaySessionManagerAsync()
    {
        MultiplaySessionManagerOptions options = new MultiplaySessionManagerOptions { };

        return await MultiplayerServerService.Instance.StartMultiplaySessionManagerAsync(options);
    }




}
