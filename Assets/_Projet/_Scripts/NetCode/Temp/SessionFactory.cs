using GameNetwork.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;
using static Unity.NetCode.ClientServerBootstrap;

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

            session.PlayerJoined += OnPlayerJoined;
            session.PlayerLeaving += OnPlayerLeaving;
            session.PlayerHasLeft += OnPlayerHasLeft;
            session.RemovedFromSession += OnRemovedFromSession;
            session.SessionPropertiesChanged += OnSessionPropertiesChanged;
            session.PlayerPropertiesChanged += OnPlayerPropertiesChanged;
            session.StateChanged += OnStateChanged;
            session.Deleted += OnSessionDeleted;

            Debug.Log($"[SessionTransportHelper] Creating server session with options: MaxPlayers={options.MaxPlayers}");
            Debug.Log($"[SessionTransportHelper] IP: {ip}, Port: {port}, IsClientLocal: {isClientLocal}");
            Debug.Log($"[ServerSessionFactory] Created session with code: {session.Id}");
            Debug.Log($"[ServerSessionFactory] Created session with name: {session.Name}");
            Debug.Log($"[ServerSessionFactory] Created session with NB properties: {session.Properties.Count}");

            for (int i = 0; i < session.Players.Count; i++)
            {
                Debug.Log($"[SessionStatusSystem] → {i} start  Player in game: {session.Players[i].Id}");
            }

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

    private static void OnPlayerPropertiesChanged()
    {
        //session.RefreshAsync();
    }

    private static void OnSessionDeleted()
    {
        Debug.Log($"[OnSessionDeleted] session deleted.");
        PlayerHelpers.ClearTeams();
    }

    private static void OnPlayerJoined(string playerId)
    {
        Debug.Log($"[OnPlayerJoined] Player with id {playerId} is joined the session.");
        var listCorpo = PlayerHelpers.GetPlayersByTeamOnServer(TeamSideType.Corpo);
        Debug.Log($"[SessionStatusSystem] → CountTeamCorpo roster size: {listCorpo.Count}");
        var listNatif = PlayerHelpers.GetPlayersByTeamOnServer(TeamSideType.Natif);
        Debug.Log($"[SessionStatusSystem] → CountTeamNatif roster size: {listNatif.Count}");
        
        Debug.Log($"session.Players.Count {session.Players.Count}");

        for (int i = 0; i < session.Players.Count; i++)
        {
            Debug.Log($"[SessionStatusSystem] → {i} Player in game: {session.Players[i].Id}");
        }

        foreach (var item in listCorpo)
        {
            Debug.Log($"[SessionStatusSystem] → TeamCorpo: {item.Id}");
        }

        foreach (var item in listNatif)
        {
            Debug.Log($"[SessionStatusSystem] → TeamNatif: {item.Id}");
        }
    }

    static private void OnStateChanged(SessionState state)
    {
        Debug.Log($"[SessionStatusSystem] state {state}");
    }

    static private void OnPlayerLeaving(string playerId)
    {
        Debug.Log($"[OnPlayerLeaving] Player with NetworkId {playerId} is leaving the session.");
        //session.RefreshAsync();
    }

    static private async void OnPlayerHasLeft(string playerId)
    {
        Debug.Log($"[OnPlayerHasLeft] Player with NetworkId {playerId} has left the session.");
        await session.RemovePlayerAsync(playerId);
        //await session.RefreshAsync();

        var listCorpo = PlayerHelpers.GetPlayersByTeamOnServer(TeamSideType.Corpo);
        Debug.Log($"[SessionStatusSystem] OnPlayerHasLeft → CountTeamCorpo roster size: {listCorpo.Count}");
        var listNatif = PlayerHelpers.GetPlayersByTeamOnServer(TeamSideType.Natif);
        Debug.Log($"[SessionStatusSystem] OnPlayerHasLeft → CountTeamNatif roster size: {listNatif.Count}");

        var listNeutre = PlayerHelpers.GetPlayersByTeamOnServer(TeamSideType.Neutre);
        Debug.Log($"[SessionStatusSystem] OnPlayerHasLeft → CountTeamNatif roster size: {listNeutre.Count}");

        if (listCorpo.Count > 0)
        {
            foreach (var playersCorpo in listCorpo)
            {
                Debug.Log($"[SessionStatusSystem] CORPO := {playerId}→ COMPARING : {playersCorpo.Id}");

                if (playersCorpo.Id == playerId)
                {
                    Debug.Log($"[SessionStatusSystem] → found corpo : {playersCorpo.Id}");
                    PlayerHelpers.RemovePlayer(playersCorpo.Id);
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
                Debug.Log($"[SessionStatusSystem] NATIF := {playerId}→ COMPARING : {playersNatif.Id}");
                if (playersNatif.Id == playerId)
                {
                    Debug.Log($"[SessionStatusSystem] → found Natif : {playersNatif.Id}");
                    PlayerHelpers.RemovePlayer(playersNatif.Id);
                    break;
                }
            }
        }
        else
        {
            Debug.Log($"[SessionStatusSystem]   – No players in Natif to check");
        }

        if (listNeutre.Count > 0)
        {
            foreach (var playersNeutre in listNeutre)
            {
                Debug.Log($"[SessionStatusSystem] NATIF := {playerId}→ COMPARING : {playersNeutre.Id}");
                if (playersNeutre.Id == playerId)
                {
                    Debug.Log($"[SessionStatusSystem] → found Natif : {playersNeutre.Id}");
                    PlayerHelpers.RemovePlayer(playersNeutre.Id);
                    break;
                }
            }
        }
        else
        {
            Debug.Log($"[SessionStatusSystem]   – No players in Natif to check");
        }
    }

    static private void OnRemovedFromSession()
    {
        Debug.Log("[SessionStatusSystem] Current client has been removed from the session.");
        //session.RefreshAsync();
    }

    static private void OnSessionPropertiesChanged()
    {
        Debug.Log("[OnSessionPropertiesChanged] Session properties have been updated.");
    }
}
