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
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using static Unity.NetCode.ClientServerBootstrap;

public class GameManager : Singleton<GameManager>
{
    public enum GlobalGameState { MainMenu, Loading, InGame }
    public GlobalGameState GameState { get; set; }

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
            serverSession = await ServerSessionFactory.CreateServerSession(ClientTransportHelper.CurrentIP, ClientTransportHelper.CurrentPort, ClientTransportHelper.isClientLocal);
        }


    }

    public void Update()
    {

    }
    public async Task Play()
    {
        await ClientTransportHelper.StartServicesAsync();
        clientConnectionSettings = await connectionHandler.Connect(loadingToken.Token);

        GameState = GlobalGameState.InGame;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("GameManager: Session is full. Transitioning to gameplay.");
        GameState = GlobalGameState.InGame;

        Debug.Log($"Nb of players: {GetCurrentNbOfPlayersInSession()}");


        for (int i = 0; i < clientConnectionSettings.Session.Players.Count; i++)
        {
            var player = clientConnectionSettings.Session.Players[i];

            foreach (var property in player.Properties)
            {
                Debug.Log($"Player {player.Id}, team {property.Value.Value}");
            }
        }
    }



    public bool IsSessionFull()
    {
        if (clientConnectionSettings != null)
        {
            return clientConnectionSettings.Session.AvailableSlots == 0;
        }
        return false;
    }
    public List<IReadOnlyPlayer> GetPlayersByTeam(string teamName)
    {
        List<IReadOnlyPlayer> playersInTeam = new List<IReadOnlyPlayer>();
        for (int i = 0; i < GetCurrentNbOfPlayersInSession(); i++)
        {
            var player = clientConnectionSettings.Session.Players[i];

            foreach (var property in player.Properties)
            {
                if (property.Value.Value == teamName)
                {
                    playersInTeam.Add(player);
                    break;
                }
            }
        }
        return playersInTeam;
    }

    public List<IReadOnlyPlayer> GetAllPlayers()
    {
        List<IReadOnlyPlayer> allPlayers = new List<IReadOnlyPlayer>();
        for (int i = 0; i < GetCurrentNbOfPlayersInSession(); i++)
        {
            allPlayers.Add(clientConnectionSettings.Session.Players[i]);
        }
        return allPlayers;
    }

    public int GetCurrentNbOfPlayersInSession()
    {

        if (clientConnectionSettings != null)
        {
            if (RequestedPlayType == PlayType.Client)
            {
                return clientConnectionSettings.Session.Players.Count - 1;
            }
            else if (RequestedPlayType == PlayType.ClientAndServer)
            {
                return clientConnectionSettings.Session.Players.Count;
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

    private async void OnApplicationQuit()
    {
        await DisconnectAndUnloadWorlds();
    }
}
