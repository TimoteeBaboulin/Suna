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
using Unity.Services.Matchmaker;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using static Unity.NetCode.ClientServerBootstrap;

public class GameManager : Singleton<GameManager>
{
    public enum GlobalGameState { MainMenu, Loading, InGame }
    public GlobalGameState GameState { get; private set; }

    public string SessionID { get; private set; }

    private ClientTransportHelper clientConnectionSettings;
    private CancellationTokenSource loadingToken;
    private ConnectionHandlerNew connectionHandler;
    private ClientTransportHelper serverSession;

    protected override void Awake()
    {
        //ClientSessionCreationCommand command = new ClientSessionCreationCommand() { createNewSession = true };
        //RpcUtils.SendDefaultToServerRPC(ref command);
    }

    private async void Start()
    {
        connectionHandler = FindFirstObjectByType<ConnectionHandlerNew>();
        loadingToken = new CancellationTokenSource();

        if (Application.platform == RuntimePlatform.WindowsServer || RequestedPlayType == PlayType.Server)
        {
            await ClientTransportHelper.StartServicesAsync();
            Debug.Log($"Port in GameManager : {AutoConnectPort}");
            serverSession = await ServerSessionFactory.CreateServerSession(connectionHandler.IP, connectionHandler.Port, connectionHandler.ClientLocal);
        }
    }
    public async Task Play()
    {
        await ClientTransportHelper.StartServicesAsync();
        await QuerySessionsAsync();

        SessionID = (RequestedPlayType == PlayType.ClientAndServer) ? "0" : SessionID;
        Debug.Log($"GameManager: Using session code: {SessionID}");

        //clientConnectionSettings = await connectionHandler.ConnectToSessionAsync(loadingToken.Token, SessionID);
        clientConnectionSettings = await connectionHandler.ConnectToSessionAsync(loadingToken.Token, SessionID);

        GameState = GlobalGameState.InGame;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("GameManager: Session is full. Transitioning to gameplay.");
        GameState = GlobalGameState.InGame;

        var matchmakingResults = clientConnectionSettings.Session.GetMatchmakingResults();
        foreach (var team in matchmakingResults.MatchProperties.Teams)
        {
            foreach (var player in team.PlayerIds)
            {
                Debug.Log($"team {team.TeamName} Player ID {player}");
            }
        }
    }

    public async void OnClickMatchmakeButton()
    {
        var matchOptions = new MatchmakerOptions
        {
            QueueName = ClientTransportHelper.GlobalQueueName,
        };

        var sessionOptions = new SessionOptions{  };

        ClientTransportHelper helper = new ClientTransportHelper(connectionHandler.IP, connectionHandler.Port, connectionHandler.ClientLocal);
        ClientTransportHelper result = await helper.MatchmakeSessionAsync(matchOptions, sessionOptions);

        if (result == null)
        {
            Debug.LogError("Failed to matchmake session.");
            return;
        }

        Debug.Log($"Successfully matched session: {result.Session.Id}");
    }

    private async void OnClickQuickJoinButton()
    {
        var quickJoinOptions = new QuickJoinOptions
        {
        };

        var sessionOptions = new SessionOptions
        {
            MaxPlayers = 4
        };

        ClientTransportHelper helper = new ClientTransportHelper("127.0.0.1", 7979, false);
        ClientTransportHelper result = await helper.QuickJoinSessionAsync(quickJoinOptions, sessionOptions);

        if (result == null)
        {
            Debug.LogError("Failed to quick-join session.");
            return;
        }

        Debug.Log($"Successfully quick-joined session: {result.Session.Id}");
    }

    private bool IsSessionFull(ISession session)
    {
        return session.AvailableSlots == 0;
    }

    private async Task QuerySessionsAsync()
    {
        var queryOptions = new QuerySessionsOptions
        {
        };

        QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);

        if (results == null || results.Sessions.Count == 0)
        {
            SessionID = "0";
            Debug.Log("No sessions found.");
            return;
        }


        foreach (var session in results.Sessions)
        {
            if (session.AvailableSlots != 0)
            {
                SessionID = session.Id;
                Debug.Log($"Players: {session.AvailableSlots}/{session.MaxPlayers}");
                Debug.Log($"Found session ID: {session.Id}");
                Debug.Log($"Session code: {session.Id}");
            }
            else
            {
                ClientSessionCreationCommand command = new ClientSessionCreationCommand() { createNewSession = true };
                RpcUtils.SendClientToServerRpc(ref command);

                Debug.Log($"Players: {session.AvailableSlots}/{session.MaxPlayers}");
                Debug.Log($"Found session ID: {session.Id}");
                Debug.Log($"Session code: {session.Id}");
            }
        }
        //var firstSession = results.Sessions[0];
        //SessionID = firstSession.Id;

        Debug.Log($"SessionId is {SessionID}");
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

    public async Task DisconnectAndUnloadWorlds()
    {
        ClientTransportHelper.State = ClientConnectionState.NotConnected;

        bool requestedDisconnect = false;
        foreach (var world in World.All)
        {
            if (world.IsClient())
            {
                using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
                if (query.TryGetSingletonEntity<NetworkId>(out var networkId))
                {
                    requestedDisconnect = true;
                    world.EntityManager.AddComponentData(networkId, new NetworkStreamRequestDisconnect());
                }
            }
        }

        if (requestedDisconnect)
            await Awaitable.NextFrameAsync();

        await LeaveSessionAsync();
        await DestroyGameSessionWorlds();
        await LoadUtils.UnloadScenesAsync("MultiplayerTest");
    }

    public async Task LeaveSessionAsync()
    {
        if (clientConnectionSettings != null)
        {
            clientConnectionSettings.Session.RemovedFromSession += OnSessionLeft;
            await clientConnectionSettings.Session.LeaveAsync();
        }
    }

    public void OnSessionLeft()
    {
        clientConnectionSettings = null;
    }

    static async Task DestroyGameSessionWorlds()
    {
        await Awaitable.EndOfFrameAsync();


        for (var i = World.All.Count - 1; i >= 0; i--)
        {
            var world = World.All[i];

            if (world == World.DefaultGameObjectInjectionWorld) { continue; }

            if (world.IsClient())
            {
                world.Dispose();
            }
        }
    }
}
