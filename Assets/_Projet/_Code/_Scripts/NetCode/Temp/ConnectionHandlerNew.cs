using GameNetwork;
using GameNetwork.Utils;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;

public class ConnectionHandlerNew : MonoBehaviour
{
    private enum RoleType { ClientServer, Server, Client }

    [Header("Connection Settings")]
    public bool isClientLocal = false;
    [Tooltip("IP to reach/to connect on")]
    [SerializeField] private string _ip = "51.210.222.138"; // default remote IP
    private ushort _port = 7979;
    private string _localIp = "127.0.0.1";

    private RoleType _role = RoleType.ClientServer;
    private World _serverWorld = null;
    private World _clientWorld = null;
    private SessionTransportHelper connection = null;
    public NetworkEndpoint ClientEndpoint { get; private set; }
    public NetworkEndpoint ServerEndpoint { get; private set; }
    public string IP { get; private set; } = "51.210.222.138";
    public ushort Port => _port;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Determine role from your bootstrap settings.
        if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer)
            _role = RoleType.ClientServer;
        else if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
            _role = RoleType.Server;
        else if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Client)
            _role = RoleType.Client;

        Debug.Log($"ConnectionHandlerNew: Role is {_role}");
    }

    /// <summary>
    /// Handles connection settings and matchmaking.
    /// </summary>
    public async Task<SessionTransportHelper> ConnectMatchmakingAsync(CancellationToken token, string sessionID)
    {
        // STEP 1: Start Loading
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.StartLoading);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Check if worlds already exist.
        if (_clientWorld != null)
        {
            Debug.Log($"{_clientWorld} already created!");
            return null;
        }

        if (_serverWorld != null)
        {
            Debug.Log($"{_serverWorld} already created!");
            return null;
        }

        // STEP 2: Initialize Connection / Create Worlds
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.InitializeConnection);
        switch (_role)
        {
            case RoleType.ClientServer:
                _clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                _serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
                break;
            case RoleType.Server:
                _serverWorld = SessionTransportHelper.ServerWorld;
                ServerConsole.Log(ServerConsole.LogType.Info, $"Server {ClientServerBootstrap.ServerWorld}, {_serverWorld}");
                Debug.Log($"Server {ClientServerBootstrap.ServerWorld}, {_serverWorld}");
                break;
            case RoleType.Client:
                _clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                break;
            default:
                Debug.LogError("ConnectionHandlerNew: No valid role specified.");
                break;
        }

        // Optionally dispose any old simulation worlds.
        DestroySimulationWorld();

        // STEP 3: Setup server world if applicable.
        if (_serverWorld != null)
        {
            World.DefaultGameObjectInjectionWorld = _serverWorld;
            ServerEndpoint = NetworkEndpoint.AnyIpv4.WithPort(_port);
            {
                using EntityQuery networkDriverQuery = _serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(ServerEndpoint);
            }

            ServerConsole.Log(ServerConsole.LogType.Info, $"ServerOn {ServerEndpoint.Address}");
        }

        // STEP 4: Setup client world and perform matchmaking.
        if (_clientWorld != null)
        {
            World.DefaultGameObjectInjectionWorld = _clientWorld;
            // Choose IP based on role. For client-only, use remote IP; for client/server, use local.
            IP = (_role == RoleType.ClientServer || isClientLocal) ? _localIp : _ip;

            // Create an instance of ClientConnection with these settings.
            SessionTransportHelper clientConn = new SessionTransportHelper(IP, _port, isClientLocal);

            SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.LookingForMatch);
            Debug.Log("ConnectionHandlerNew: Starting matchmaking...");

            // Await matchmaking.
            //ClientConnection connection = await clientConn.JoinOrCreateMatchmakerGameAsync(token);
            Debug.Log($"sessionID in Handler : {sessionID}");
            //connection = await new SessionTransportHelper(_ip, _port, isClientLocal)
            //                                                 .CreateOrJoinSessionAsync(sessionID, token);
            connection = await new SessionTransportHelper(_ip, _port, isClientLocal).JoinSessionByIdAsync(sessionID, token);
            SessionTransportHelper.SessionID = sessionID;
            //ClientEndpoint = connection.ConnectEndpoint;

            //var queryClient = _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            //while (queryClient.CalculateEntityCount() == 0)
            //{
            //    await Task.Yield();
            //}
            //NetworkStreamDriver clientDriver = queryClient.GetSingleton<NetworkStreamDriver>();
            //clientDriver.Connect(_clientWorld.EntityManager, ClientEndpoint);
            //queryClient.SetSingleton(clientDriver);

            ClientEndpoint = NetworkEndpoint.Parse(IP, _port);
            {
                using EntityQuery networkDriverQuery = _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(_clientWorld.EntityManager, ClientEndpoint);
            }

            Debug.Log($"ConnectionHandlerNew: Client connecting to {ClientEndpoint.Address}");
        }

        // STEP 5: Load subscenes.
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.LoadGameScene);
        SubScene[] subScenes = FindObjectsByType<SubScene>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (_serverWorld != null)
            await LoadSubScenesAsync(subScenes, _serverWorld);
        if (_clientWorld != null)
            await LoadSubScenesAsync(subScenes, _clientWorld);

        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.LoadingDone);
        Debug.Log("ConnectionHandlerNew: Finished loading worlds and subscenes.");
        return connection;
        //return null;
    }

    private async Task LoadSubScenesAsync(SubScene[] subScenes, World world)
    {
        while (!world.IsCreated)
        {
            await Task.Yield();
        }
        if (subScenes != null)
        {
            for (int i = 0; i < subScenes.Length; i++)
            {
                SceneLoadFlags flag = SceneLoadFlags.BlockOnStreamIn;
#if UNITY_EDITOR
                flag = SceneLoadFlags.BlockOnImport;
#endif
                SceneSystem.LoadParameters loadParams = new SceneSystem.LoadParameters() { Flags = flag };
                Entity sceneEntity = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(subScenes[i].SceneGUID.Value), loadParams);
                while (!SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
                {
                    world.Update();
                    await Task.Yield();
                }
                Debug.Log($"ConnectionHandlerNew: Loaded subscene {subScenes[i].name} in world {world.Name}");
            }
        }
    }

    private void DestroySimulationWorld()
    {
        foreach (var world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }
    }
}
