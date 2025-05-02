using GameNetwork;
using GameNetwork.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using static Unity.NetCode.ClientServerBootstrap;

public class GameManager : Singleton<GameManager>
{
    public enum GlobalGameState { MainMenu, Loading, InGame }
    public GlobalGameState GameState { get; set; }
    private ISession currentSession => sessionTransport.Session;

    private ClientTransportHelper sessionTransport;
    private CancellationTokenSource loadingToken;
    private ConnectionHandlerNew connectionHandler;

    private int countTeamNatif;
    private int countTeamCorpo;

    protected override void Awake()
    {
        //ClientSessionCreationCommand command = new ClientSessionCreationCommand() { createNewSession = true };
        //RpcUtils.SendDefaultToServerRPC(ref command);
    }

    private async void Start()
    {
        connectionHandler = FindFirstObjectByType<ConnectionHandlerNew>();
        loadingToken = new CancellationTokenSource();

        if (RequestedPlayType == PlayType.Server)
        {
            await ClientTransportHelper.StartServicesAsync();
            Debug.Log($"Port in GameManager : {AutoConnectPort}");
            sessionTransport = await ServerSessionFactory.CreateServerSession(ClientTransportHelper.CurrentIP, ClientTransportHelper.CurrentPort, ClientTransportHelper.isClientLocal);
        }
    }

    public void Update()
    {

    }
    public async Task Play()
    {
        await ClientTransportHelper.StartServicesAsync();
        sessionTransport = await connectionHandler.Connect(loadingToken.Token);

        GameState = GlobalGameState.InGame;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("GameManager: Session is full. Transitioning to gameplay.");
        GameState = GlobalGameState.InGame;

        Debug.Log($"Nb of players: {GetCurrentNbOfPlayersInSession()}");
        Debug.Log($"Count of current player PROPERTIES: {currentSession.CurrentPlayer.Properties.Count}");
    }

    public List<IReadOnlyPlayer> GetAllPlayers()
    {
        List<IReadOnlyPlayer> allPlayers = new List<IReadOnlyPlayer>();
        for (int i = 0; i < GetCurrentNbOfPlayersInSession(); i++)
        {
            allPlayers.Add(currentSession.Players[i]);
        }
        return allPlayers;
    }

    public int GetCurrentNbOfPlayersInSession()
    {
        if (sessionTransport != null)
        {
            if (RequestedPlayType == PlayType.Client)
            {
                return currentSession.Players.Count - 1;
            }
            else if (RequestedPlayType == PlayType.ClientAndServer)
            {
                return currentSession.Players.Count;
            }
        }
        return 0;
    }


    private async void OnClickMatchmakeButton()
    {
        var matchOptions = new MatchmakerOptions
        {
            QueueName = "myQueue",
        };

        var sessionOptions = new SessionOptions
        {
            MaxPlayers = 4
        };

        ClientTransportHelper helper = new ClientTransportHelper();
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

        ClientTransportHelper helper = new ClientTransportHelper();
        ClientTransportHelper result = await helper.QuickJoinSessionAsync(quickJoinOptions, sessionOptions);

        if (result == null)
        {
            Debug.LogError("Failed to quick-join session.");
            return;
        }

        Debug.Log($"Successfully quick-joined session: {result.Session.Id}");
    }


    private async void OnApplicationQuit()
    {
        Debug.Log("[OnApplicationQuit] Application is quitting – disconnecting and unloading worlds.");
        //LoadUtils.ResetAllCharacterComponents();

        loadingToken.Cancel();

#if !UNITY_EDITOR
        await LoadUtils.QuitAsync();
#endif
    }
}
