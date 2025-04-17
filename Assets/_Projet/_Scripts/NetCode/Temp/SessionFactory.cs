using GameNetwork.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;
using static Unity.NetCode.ClientServerBootstrap;
using static UnityEngine.CullingGroup;

public class ServerSessionFactory
{
    private static ServerSessionFactory instance;
    private static IHostSession session;
    private ServerSessionFactory() { }

    public static async Task<ClientTransportHelper> CreateServerSession(string ip, ushort port, bool isClientLocal)
    {
        instance = new ServerSessionFactory();
        try
        {
            AutoConnectPort = port;
            SessionOptions options = new SessionOptions
            {
                MaxPlayers = ClientTransportHelper.MaxNbOfPlayers,
                Name = ClientTransportHelper.CurrentPort.ToString()
            };
            ClientTransportHelper transportHelper = new ClientTransportHelper();
            ClientTransportHelper serverSession = await transportHelper.CreateServerSessionAsync(options);

            session = serverSession.Session.AsHost();

            session.PlayerLeaving += OnPlayerLeaving;
            session.PlayerHasLeft += OnPlayerHasLeft;
            session.RemovedFromSession += OnRemovedFromSession;
            session.SessionPropertiesChanged += OnSessionPropertiesChanged;
            session.StateChanged += OnStateChanged;

            Debug.Log($"[SessionTransportHelper] Creating server session with options: MaxPlayers={options.MaxPlayers}");
            Debug.Log($"[SessionTransportHelper] IP: {ip}, Port: {port}, IsClientLocal: {isClientLocal}");
            Debug.Log($"[ServerSessionFactory] Created session with code: {session.Id}");
            Debug.Log($"[ServerSessionFactory] Created session with name: {session.Name}");
            Debug.Log($"[ServerSessionFactory] Created session with NB properties: {session.Properties.Count}");

            foreach (var property in session.Properties)
            {
                Debug.Log($"[ServerSessionFactory] Created session with property: {property.Key} : {property.Value.Value}");
            }
            return serverSession;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerSessionFactory] Error creating session: {ex}");
            return null;
        }
    }

    static private void OnStateChanged(SessionState state)
    {
        Debug.Log($"[SessionStatusSystem] state {state}");
    }

    static private void OnPlayerLeaving(string playerId)
    {
        Debug.Log($"[OnPlayerLeaving] Player with NetworkId {playerId} is leaving the session.");
    }

    static private void OnPlayerHasLeft(string playerId)
    {
        Debug.Log($"[SessionStatusSystem] Player with NetworkId {playerId} has left the session.");
        var listCorpo = PlayerHelpers.GetPlayersByTeam(TeamSideType.Corpo);
        Debug.Log($"[SessionStatusSystem] → CountTeamCorpo roster size: {listCorpo.Count}");
        var listNatif = PlayerHelpers.GetPlayersByTeam(TeamSideType.Natif);
        Debug.Log($"[SessionStatusSystem] → CountTeamNatif roster size: {listNatif.Count}");

        if (listCorpo.Count > 0)
        {
            foreach (var playersCorpo in listCorpo)
            {
                if (playersCorpo.Id == playerId)
                {
                    Debug.Log($"[SessionStatusSystem] → found corpo : {playerId}");
                    PlayerHelpers.RemovePlayer(playerId);
                    break;
                }
            }
        }
        else
        {
            Debug.Log($"[SessionStatusSystem]   – No players in Corpo to check");
        }

        if (listNatif.Count > 0)
        {
            foreach (var playersNatif in listNatif)
            {
                if (playersNatif.Id == playerId)
                {
                    Debug.Log($"[SessionStatusSystem] → found Natif : {playerId}");
                    PlayerHelpers.RemovePlayer(playerId);
                    break;
                }
            }
        }
        else
        {
            Debug.Log($"[SessionStatusSystem]   – No players in Natif to check");
        }
        session.RemovePlayerAsync(playerId);
        //session.RefreshAsync();
    }

    static private void OnRemovedFromSession()
    {
        Debug.Log("[SessionStatusSystem] Current client has been removed from the session.");
    }

    static private void OnSessionPropertiesChanged()
    {
        Debug.Log("[OnSessionPropertiesChanged] Session properties have been updated.");
    }
}
