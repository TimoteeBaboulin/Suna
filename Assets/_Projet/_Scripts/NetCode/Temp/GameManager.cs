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

        if (currentSession is IHostSession hostSession)
        {
            hostSession.PlayerJoined += OnPlayerJoined;
            Debug.Log("[Team Assignment] PlayerJoined listener attached.");

            // Assign teams to any existing players (like the host)
            foreach (var player in currentSession.Players)
            {
                if (!player.Properties.TryGetValue("team", out var prop) || prop.Value == "none")
                {
                    AssignTeamToPlayer(player, currentSession.Players);
                }
            }
        }

        var localPlayer = currentSession.CurrentPlayer;
        if (localPlayer.Properties.TryGetValue("team", out PlayerProperty localTeam))
        {
            Debug.Log($"[Play] Local player team: {localTeam.Value}");
        }
    }

    private void AssignTeamToPlayer(IReadOnlyPlayer readOnlyPlayer, IReadOnlyList<IReadOnlyPlayer> allPlayers)
    {
        int countTeamA = 0;
        int countTeamB = 0;

        foreach (var p in allPlayers)
        {
            if (p.Properties.TryGetValue("team", out PlayerProperty prop))
            {
                if (prop.Value == "Corpo")
                    countTeamA++;
                else if (prop.Value == "Natif")
                    countTeamB++;
            }
        }

        string assignedTeam = (countTeamA == 0 && countTeamB == 0)
            ? ((UnityEngine.Random.value < 0.5f) ? "Corpo" : "Natif")
            : (countTeamA <= countTeamB ? "Corpo" : "Natif");

        if (readOnlyPlayer is IPlayer player)
        {
            player.SetProperty("team", new PlayerProperty(assignedTeam, VisibilityPropertyOptions.Public));
            Debug.Log($"[Team Assignment] Assigned Player {player.AllocationId} to team {assignedTeam}");

            UpdateTeamCountInSession(assignedTeam, player.Id);
        }
        else
        {
            Debug.LogError("[Team Assignment] Cannot assign team: player instance is not modifiable.");
        }
    }

    private void UpdateTeamCountInSession(string assignedTeam, string playerId)
    {
        if (currentSession is IHostSession hostSession)
        {
            if (assignedTeam == "Corpo")
            {
                var countTeamCorpoProp = hostSession.Properties["CountTeamCorpo"];
                int currentCountCorpo = int.Parse(countTeamCorpoProp.Value);
                hostSession.SetProperty("CountTeamCorpo", new SessionProperty((currentCountCorpo + 1).ToString(), VisibilityPropertyOptions.Public));
                hostSession.SavePropertiesAsync();
                hostSession.SavePlayerDataAsync(playerId);
            }
            else if (assignedTeam == "Natif")
            {
                var countTeamNatifProp = hostSession.Properties["CountTeamNatif"];
                int currentCountNatif = int.Parse(countTeamNatifProp.Value);
                hostSession.SetProperty("CountTeamNatif", new SessionProperty((currentCountNatif + 1).ToString(), VisibilityPropertyOptions.Public));
                hostSession.SavePropertiesAsync();
                hostSession.SavePlayerDataAsync(playerId);
            }

            Debug.Log($"Updated Team Counts: Corpo = {hostSession.Properties["CountTeamCorpo"].Value}, Natif = {hostSession.Properties["CountTeamNatif"].Value}");
        }
        else
        {
            Debug.LogError("[Team Assignment] Session is not of type IHostSession.");
        }
    }

    private void OnPlayerJoined(string playerId)
    {
        Debug.Log($"[Team Assignment] PlayerJoined triggered for ID: {playerId}");

        foreach (var p in currentSession.Players)
        {
            Debug.Log($"[Player Check] Session Player ID: {p.Id}");
        }

        var player = currentSession.Players.FirstOrDefault(p => p.Id == playerId);

        if (player != null)
        {
            Debug.Log($"[Team Assignment] Match found! Assigning team to Player ID: {player.Id}");
            AssignTeamToPlayer(player, currentSession.Players);
        }
        else
        {
            Debug.LogWarning($"[Team Assignment] No player found with ID: {playerId}");
        }
    }



    //public Dictionary<string, List<IReadOnlyPlayer>> GetPlayersByTeam()
    //{
    //    var playersByTeam = new Dictionary<string, List<IReadOnlyPlayer>>();

    //    foreach (var player in CurrentSession.Players)
    //    {
    //        if (player.Properties.TryGetValue("team", out PlayerProperty teamProp))
    //        {
    //            string team = teamProp.Value;
    //            if (!playersByTeam.ContainsKey(team))
    //            {
    //                playersByTeam[team] = new List<IReadOnlyPlayer>();
    //            }

    //            playersByTeam[team].Add(player);
    //        }
    //    }

    //    return playersByTeam;

    //    //var playersByTeam = GetPlayersByTeam();
    //    //foreach (var kvp in playersByTeam)
    //    //{
    //    //    Debug.Log($"Team {kvp.Key} players:");
    //    //    foreach (var player in kvp.Value)
    //    //    {
    //    //        Debug.Log($" - Player ID: {player.Id}");
    //    //    }
    //    //}
    //}

    //public List<IReadOnlyPlayer> GetPlayersByTeam(string teamName)
    //{
    //    var teamPlayers = new List<IReadOnlyPlayer>();

    //    foreach (var player in CurrentSession.Players)
    //    {
    //        if (player.Properties.TryGetValue("team", out PlayerProperty teamProp))
    //        {
    //            if (teamProp.Value.Equals(teamName, StringComparison.OrdinalIgnoreCase))
    //            {
    //                teamPlayers.Add(player);
    //            }
    //        }
    //    }
    //    return teamPlayers;
    //}

    public bool IsSessionFull()
    {
        if (sessionTransport != null)
        {
            return currentSession.AvailableSlots == 0;
        }
        return false;
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
