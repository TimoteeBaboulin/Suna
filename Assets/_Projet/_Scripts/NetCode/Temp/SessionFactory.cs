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
    private static ServerSessionFactory instance;
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

            ISession session = serverSession.Session;

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

    //// Custom events you may want to expose
    //public event Action<string> PlayerDisconnected;
    //public event Action<string> PlayerReconnected;
    //public event Action<SessionState> SessionStateChanged;
    ///// <summary>
    ///// Subscribes to events on the provided session to monitor when players disconnect or the session reconnects.
    ///// </summary>
    ///// <param name="session">The session to subscribe to.</param>
    //public void SubscribeToSessionEvents(ISession session)
    //{
    //    session.PlayerLeaving += OnPlayerLeavingHandler;
    //    session.PlayerHasLeft += OnPlayerHasLeftHandler;
    //    session.StateChanged += async (newState) => await OnSessionStateChangedAsync(session, newState);
    //}

    //private void OnPlayerLeavingHandler(string playerId)
    //{
    //    Debug.Log($"[ServerSessionFactory] Player '{playerId}' is leaving.");
    //    PlayerDisconnected?.Invoke(playerId);
    //}

    //private void OnPlayerHasLeftHandler(string playerId)
    //{
    //    Debug.Log($"[ServerSessionFactory] Player '{playerId}' has left the session.");
    //    PlayerDisconnected?.Invoke(playerId);
    //}

    ///// <summary>
    ///// Handles state changes and attempts reconnection if the session state indicates disconnection.
    ///// </summary>
    ///// <param name="session">The session whose state changed.</param>
    ///// <param name="newState">The new state of the session.</param>
    //private async Task OnSessionStateChangedAsync(ISession session, SessionState newState)
    //{
    //    Debug.Log($"[ServerSessionFactory] Session state changed to: {newState}");
    //    SessionStateChanged?.Invoke(newState);

    //    if (newState == SessionState.Disconnected)
    //    {
    //        Debug.Log("[ServerSessionFactory] Session is disconnected. Attempting to reconnect...");

    //        try
    //        {
    //            await session.ReconnectAsync();
    //            Debug.Log("[ServerSessionFactory] Session reconnected successfully.");
    //            PlayerReconnected?.Invoke(session.CurrentPlayer?.Id);
    //        }
    //        catch (SessionException ex)
    //        {
    //            Debug.LogError($"[ServerSessionFactory] Failed to reconnect session: {ex.Message}");
    //        }
    //    }
    //}
}
