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

        if (Application.platform == RuntimePlatform.WindowsServer || RequestedPlayType == PlayType.Server)
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

        var localPlayer = currentSession.CurrentPlayer;
        if (localPlayer.Properties.TryGetValue("team", out PlayerProperty localTeam))
        {
            Debug.Log($"[Play] Local player team: {localTeam.Value}");
        }
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

    public async Task DisconnectAndUnloadWorlds()
    {
        // Mark the connection state as not connected.
        ClientTransportHelper.State = ClientConnectionState.NotConnected;

        // bool requestedDisconnect = false;

        // Loop over all worlds that are client worlds.
        foreach (var world in World.All)
        {
            if (world.IsClient())
            {
                // Instead of trying to get a singleton, get all entities with NetworkId.
                using (var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>()))
                {
                    var entities = query.ToEntityArray(Allocator.Temp);
                    foreach (var entity in entities)
                    {
                        // Log which entity is getting the disconnect request.
                        Debug.Log($"[Disconnect] Adding disconnect request to entity: {entity}");
                        world.EntityManager.AddComponentData(entity, new NetworkStreamRequestDisconnect());
                        // requestedDisconnect = true;
                    }
                    entities.Dispose();
                }
            }
        }

        //// If we sent any disconnect request, wait a few frames or a short period to let the network system process it.
        //if (requestedDisconnect)
        //{
        //    // You might try waiting for two frames or a fixed time delay instead of just the next frame.
        //    await Awaitable.NextFrameAsync();
        //    // Alternatively, await a delay: await Awaitable.WaitForSeconds(0.5f);
        //}

        // Proceed with leaving the session.
        await LeaveSessionAsync();

        //// Optionally, wait a bit more to ensure the disconnection is fully processed.
        //// For example, wait until clientConnectionSettings gets cleared by the callback.
        //int attempts = 0;
        //while (clientConnectionSettings != null && attempts < 10)
        //{
        //    await Awaitable.NextFrameAsync();
        //    attempts++;
        //}

        //await DestroyGameSessionWorlds();
        //await LoadUtils.UnloadScenesAsync("MultiplayerTest");
    }

    public async Task LeaveSessionAsync()
    {
        if (sessionTransport != null)
        {
            // Log that we are initiating session leave.
            Debug.Log("[LeaveSessionAsync] Initiating leave process for session.");
            currentSession.RemovedFromSession += OnSessionLeft;
            await currentSession.LeaveAsync();
        }
    }

    public void OnSessionLeft()
    {
        Debug.Log("[OnSessionLeft] Session left successfully.");
        sessionTransport = null;
    }

    static async Task DestroyGameSessionWorlds()
    {
        // Ensure the network systems have time to process all disconnects.
        await Awaitable.EndOfFrameAsync();

        for (var i = World.All.Count - 1; i >= 0; i--)
        {
            var world = World.All[i];

            // Do not dispose the default world.
            if (world == World.DefaultGameObjectInjectionWorld)
                continue;

            if (world.IsClient())
            {
                Debug.Log($"[DestroyGameSessionWorlds] Disposing world: {world.Name}");
                world.Dispose();
            }
        }
    }

    private async void OnApplicationQuit()
    {
        // Log that the application is quitting.
        Debug.Log("[OnApplicationQuit] Application is quitting – disconnecting and unloading worlds.");
        await DisconnectAndUnloadWorlds();
    }
}
