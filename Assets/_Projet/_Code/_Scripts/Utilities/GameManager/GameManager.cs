using GameNetwork;
using GameNetwork.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.NetCode.ClientServerBootstrap;

public class GameManager : Singleton<GameManager>
{
    public enum GlobalGameState { MainMenu, Loading, InGame }
    public int MaxNbOfPlayer = 1;
    public GlobalGameState GameState { get; private set; }

    public string SessionID { get; private set; }

    private SessionTransportHelper clientConnectionSettings;
    private CancellationTokenSource loadingToken;
    private ConnectionHandlerNew connectionHandler;
    private SessionTransportHelper serverSession;

    protected override async void Awake()
    {
        base.Awake();
        connectionHandler = FindFirstObjectByType<ConnectionHandlerNew>();
        loadingToken = new CancellationTokenSource();

        if (Application.platform == RuntimePlatform.WindowsServer || RequestedPlayType == PlayType.Server)
        {
            serverSession = await ServerSessionFactory.CreateServerSession(connectionHandler.IP, connectionHandler.Port, connectionHandler.ClientLocal);
        }

        //ClientSessionCreationCommand command = new ClientSessionCreationCommand() { createNewSession = true };
        //RpcUtils.SendDefaultToServerRPC(ref command);
    }

    public void SetSessionID(string sessionID)
    {
        // This could store the session ID internally, or trigger further logic
        Debug.Log($"GameManager received SessionID: {sessionID}");
        SessionID = sessionID;
    }
    public async void Play()
    {
        await SessionTransportHelper.StartServicesAsync();
        await QuerySessionsAsync();
        Debug.Log($"GameManager: Using session code: {SessionID}");

        clientConnectionSettings = await connectionHandler.ConnectToSessionAsync(loadingToken.Token, SessionID);
        //if (clientConnectionSettings == null)
        //{
        //    Debug.LogError("GameManager: Client connection settings are null.");
        //    return;
        //}
        //Debug.Log($"GameManager: Connected Session ID: {clientConnectionSettings.Session.Id}");

        //while (!IsSessionFull(clientConnectionSettings.Session))
        //{
        //    Debug.Log($"GameManager: Waiting for session to fill... Available slots: {clientConnectionSettings.Session.AvailableSlots}");
        //    await Task.Delay(1000, loadingToken.Token);
        //}
        Debug.Log("GameManager: Session is full. Transitioning to gameplay.");
        GameState = GlobalGameState.InGame;
        SceneManager.LoadScene("MultiplayerTest");

        {//try
         //{
         //    await ClientConnection.StartServicesAsync(); // Ensure services are initialized.
         //    Debug.Log($"GameManager: Using session ID: {SessionID}");

            //    // Use ConnectionHandlerNew to perform matchmaking with the current SessionID.
            //    clientConnectionSettings = await connectionHandler.ConnectMatchmakingAsync(loadingToken.Token, SessionID);
            //    Debug.Log($"MaxPlayers: {clientConnectionSettings.Session.MaxPlayers}");
            //    if (clientConnectionSettings == null)
            //    {
            //        Debug.LogError("GameManager: Client connection settings are null.");
            //        return;
            //    }
            //    Debug.Log($"GameManager: Connected Session ID: {clientConnectionSettings.Session.Id}");

            //    // Optionally, wait until the session is full before transitioning.
            //    //while (!IsSessionFull(clientConnectionSettings.Session))
            //    //{
            //    //    Debug.Log($"GameManager: Waiting for session to fill... {clientConnectionSettings.Session.AvailableSlots}");
            //    //    await Task.Delay(1000, loadingToken.Token);
            //    //}

            //    //while (!IsSessionFull(clientConnectionSettings.Session))
            //    //{
            //    //    await Task.Yield(); 
            //    //}
            //    //Debug.Log("GameManager: Session is full. Transitioning to gameplay.");
            //    //GameState = GlobalGameState.InGame;
            //    //SceneManager.LoadScene("MultiplayerTest");

            //    StartCoroutine(WaitUntilSessionIsFull());
            //}
            //catch (Exception ex)
            //{
            //    Debug.LogError($"GameManager: Error in Play: {ex}");
            //}
        }
    }

    private async void OnClickMatchmakeButton()
    {
        var matchOptions = new MatchmakerOptions
        {
            // e.g. your matchmaker queue name
            QueueName = "myQueue",
            // Additional matchmaking constraints...
        };

        var sessionOptions = new SessionOptions
        {
            MaxPlayers = 4
        };

        SessionTransportHelper helper = new SessionTransportHelper("127.0.0.1", 7979, false);
        SessionTransportHelper result = await helper.MatchmakeSessionAsync(matchOptions, sessionOptions);

        if (result == null)
        {
            Debug.LogError("Failed to matchmake session.");
            return;
        }

        Debug.Log($"Successfully matched session: {result.Session.Id}");
        // Proceed to connect your NetCode or NGO with the Relay info or direct IP.
    }

    private async void OnClickQuickJoinButton()
    {
        // 1. Define how quick join should behave
        var quickJoinOptions = new QuickJoinOptions
        {
        };

        // 2. Define session creation options if no session is found
        var sessionOptions = new SessionOptions
        {
            MaxPlayers = 4
        };

        SessionTransportHelper helper = new SessionTransportHelper("127.0.0.1", 7979, false);
        SessionTransportHelper result = await helper.QuickJoinSessionAsync(quickJoinOptions, sessionOptions);

        if (result == null)
        {
            Debug.LogError("Failed to quick-join session.");
            return;
        }

        Debug.Log($"Successfully quick-joined session: {result.Session.Id}");
    }

    /// <summary>
    /// Checks whether the session is full.
    /// Replace this with your actual logic based on ISession properties.
    /// </summary>
    private bool IsSessionFull(ISession session)
    {
        // For example, if session.AvailableSlots == 0 then the session is full.
        return session.AvailableSlots == 0;
    }

    private async Task QuerySessionsAsync()
    {
        try
        {
            var queryOptions = new QuerySessionsOptions
            {
                // e.g. specify filters or pagination here
            };

            QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);

            // 'results' now contains a list of sessions that match your filters.
            if (results == null || results.Sessions.Count == 0)
            {
                Debug.Log("No sessions found.");
                return;
            }


            //foreach (var session in results.Sessions)
            //{
            //    if (session.AvailableSlots != 0)
            //    {
            //        SessionID = session.Id;
            //        Debug.Log($"Players: {session.AvailableSlots}/{session.MaxPlayers}");
            //        Debug.Log($"Found session ID: {session.Id}");
            //        Debug.Log($"Session code: {session.Id}");
            //    }
            //    else
            //    {
            //        ClientSessionCreationCommand command = new ClientSessionCreationCommand() { createNewSession = true };
            //        RpcUtils.SendClientToServerRpc(ref command);

            //        Debug.Log($"Players: {session.AvailableSlots}/{session.MaxPlayers}");
            //        Debug.Log($"Found session ID: {session.Id}");
            //        Debug.Log($"Session code: {session.Id}");
            //    }
            //}
            //var firstSession = results.Sessions[0];
            //SessionID = firstSession.Id;


            // From here, you could store the session info in your own manager,
            // or pass it to an ECS system that sets a SessionInfo entity, etc.
        }
        catch (SessionException e)
        {
            Debug.LogError($"Error querying sessions: {e.Message}");
        }
    }

    IEnumerator WaitUntilSessionIsFull()
    {
        while (!IsSessionFull(clientConnectionSettings.Session))
        {
            Debug.Log($"GameManager: Current player count: {clientConnectionSettings.Session.PlayerCount}, " +
                $"available slots: {clientConnectionSettings.Session.AvailableSlots}");

            SessionData.Instance.UpdateSessionState(clientConnectionSettings.Session.PlayerCount, clientConnectionSettings.Session.AvailableSlots, clientConnectionSettings.Session);
            yield return null;
        }
        Debug.Log("GameManager: Session is full. Transitioning to gameplay.");
        GameState = GlobalGameState.InGame;
        SceneManager.LoadScene("MultiplayerTest");
    }
}
