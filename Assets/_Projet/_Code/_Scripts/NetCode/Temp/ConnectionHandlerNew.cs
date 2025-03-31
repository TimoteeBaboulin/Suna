using GameNetwork;
using GameNetwork.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using Unity.Services.Multiplayer;
using Unity_NetCode_Generated_Unity_Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ConnectionManager;
using static System.Collections.Specialized.BitVector32;
using static Unity.NetCode.ClientServerBootstrap;


public class ConnectionHandlerNew : Singleton<ConnectionHandlerNew>
{
    //[Header("Connection Settings")]
    //public bool isClientLocal = false;
    //[Tooltip("IP to reach/to connect on")]
    //[SerializeField] private string _ip = "51.210.222.138"; // default remote IP
    private ushort _port = 7979;
    //private string _localIp = "127.0.0.1";
    private string _ip;
    // private ushort _port = 7979;
    private string _localIp = "127.0.0.1";
    private bool isClientLocal;
    private ConnectionSettings connectionSettings;

    //private World _serverWorld = null;
    //private World _clientWorld = null;
    private ClientTransportHelper sessionTransport = null;
    //public NetworkEndpoint ClientEndpoint { get; private set; }
    //public NetworkEndpoint ServerEndpoint { get; private set; }
    public string IP { get; private set; } = "127.0.0.1";//"51.210.222.138";
    public ushort Port { get; private set; }
    public bool ClientLocal => isClientLocal;
    protected override void Awake()
    {
        connectionSettings = GetComponent<ConnectionSettings>();
        isClientLocal = connectionSettings.isClientLocal;
        _ip = connectionSettings.IP;
        IP = (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer || isClientLocal) ? _localIp : _ip;
        Port = connectionSettings.Port;
        DontDestroyOnLoad(gameObject);


        //AutoConnectPort = GetAvailablePort();
        //AutoConnectPort = 53867;
        //Debug.Log($"AutoConnectPort = {AutoConnectPort}");
    }

    public ushort GetAvailablePort()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        ushort port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private void Start()
    {
        // _port = GetAvailablePort();


        // Determine role from your bootstrap settings.
        //if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer)
        //    _role = RoleType.ClientServer;
        //else if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
        //    _role = RoleType.Server;
        //else if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Client)
        //    _role = RoleType.Client;

        Debug.Log($"ConnectionHandlerNew: Role is {ClientServerBootstrap.RequestedPlayType}");
    }

    /// <summary>
    /// Handles connection settings and matchmaking.
    /// </summary>
    public async Task<ClientTransportHelper> ConnectToSessionAsync(CancellationToken token, string sessionID)
    {
        // STEP 1: Start Loading
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.StartLoading);
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        //NetworkRole role;
        // Check if worlds already exist.
        //if (_clientWorld != null)
        //{
        //    Debug.Log($"{_clientWorld} already created!");
        //    return null;
        //}

        //if (_serverWorld != null)
        //{
        //    Debug.Log($"{_serverWorld} already created!");
        //    return null;
        //}

        // STEP 2: Initialize Connection / Create Worlds
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.InitializeConnection);

        CreateEntityWorlds(out var serverWorld, out var clientWorld);
        {
            // Optionally dispose any old simulation worlds.
            //DestroySimulationWorld();

            // STEP 3: Setup server world if applicable.
            //if (_serverWorld != null)
            //{
            //    World.DefaultGameObjectInjectionWorld = _serverWorld;

            //    NetworkEndpoint serverEndPoint = NetworkEndpoint.AnyIpv4.WithPort(_port);
            //    {
            //        using EntityQuery networkDriverQuery = _serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            //        //networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndPoint);
            //        try
            //        {
            //            var driverRW = networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
            //            var result = driverRW.Listen(serverEndPoint);
            //            if (!result)
            //            {
            //                Debug.LogError($"Server failed to listen on {serverEndPoint}: result={result}");
            //            }
            //        }
            //        catch (InvalidOperationException ex)
            //        {
            //            Debug.LogWarning($"Listen already called or driver already bound: {ex.Message}");
            //        }
            //    }

            //    ServerConsole.Log(ServerConsole.LogType.Info, $"ServerOn {serverEndPoint.Address}");
            //}
        }
        sessionTransport = await new ClientTransportHelper(IP, _port, isClientLocal).CreateOrJoinSessionAsync("0", token);
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.WaitingConnection);
        if (serverWorld != null)
        {
            using var drvQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var serverDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
            serverDriver.Listen(sessionTransport.ListenEndpoint);
        }

        if (clientWorld != null)
        {
            using var drvQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var clientDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
            clientDriver.Connect(clientWorld.EntityManager, sessionTransport.ConnectEndpoint);
            //await ClientTransportHelper.WaitForPlayerConnectionAsync(token);

            //ref var driver = ref SystemAPI.GetSingletonRW<NetworkStreamDriver>().ValueRW;
            //driver.Connect(EntityManager, ConnectionSettings.Instance.ConnectionEndpoint);
            //World.DefaultGameObjectInjectionWorld = clientWorld;


            //sessionTransport.ConnectEndpoint = NetworkEndpoint.Parse(_ip, _port);
            //{
            //    using EntityQuery networkDriverQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            //    networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, sessionTransport.ConnectEndpoint);
            //}
        }

        {
            // STEP 4: Setup client world and perform matchmaking.
            //if (_clientWorld != null)
            //{
            //    World.DefaultGameObjectInjectionWorld = _clientWorld;


            //    // Choose IP based on role. For client-only, use remote IP; for client/server, use local.
            //    IP = (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer || isClientLocal) ? _localIp : _ip;

            //    await Task.Yield();

            //    // Create an instance of ClientConnection with these settings.

            //    SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.LookingForMatch);
            //    Debug.Log("ConnectionHandlerNew: Starting matchmaking...");

            //    // Await matchmaking.
            //    //ClientConnection connection = await clientConn.JoinOrCreateMatchmakerGameAsync(token);
            //    Debug.Log($"sessionID in Handler : {sessionID}");
            //    //connection = await new SessionTransportHelper(_ip, _port, isClientLocal)
            //    //                                                 .CreateOrJoinSessionAsync(sessionID, token);
            //    //sessionTransport = await new SessionTransportHelper(_ip, _port, isClientLocal).JoinSessionByIdAsync(sessionID, token);
            //    //sessionTransport = await new SessionTransportHelper(_ip, _port, isClientLocal).JoinOrCreateMatchmakerGameAsync(token);
            //    //SessionTransportHelper.SessionID = sessionID;
            //    //ClientEndpoint = connection.ConnectEndpoint;

            //    //var queryClient = _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            //    //while (queryClient.CalculateEntityCount() == 0)
            //    //{
            //    //    await Task.Yield();
            //    //}
            //    //NetworkStreamDriver clientDriver = queryClient.GetSingleton<NetworkStreamDriver>();
            //    //clientDriver.Connect(_clientWorld.EntityManager, ClientEndpoint);
            //    //queryClient.SetSingleton(clientDriver);

            //    NetworkEndpoint clientEndpoint = NetworkEndpoint.Parse(IP, _port);
            //    {
            //        using (EntityQuery networkDriverQuery = _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>()))
            //        {
            //            networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(_clientWorld.EntityManager, clientEndpoint);
            //            Debug.Log($"Client connecting to {clientEndpoint.Address}");
            //            //try
            //            //{
            //            //}
            //            //catch (InvalidOperationException ex)
            //            //{
            //            //    Debug.LogWarning("Connect call skipped: " + ex.Message);
            //            //}
            //        }
            //    }

            //    Debug.Log($"ConnectionHandlerNew: Client connecting to {clientEndpoint.Address}");
            //}

            // STEP 5: Load subscenes.
            //SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.LoadGameScene);
            //SubScene[] subScenes = FindObjectsByType<SubScene>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            //if (_serverWorld != null)
            //    await LoadSubScenesAsync(subScenes, _serverWorld);
            //if (_clientWorld != null)
            //    await LoadSubScenesAsync(subScenes, _clientWorld);
        }

        //await LoadGameplayAsync(serverWorld, clientWorld);
        // STEP 5: Load subscenes.
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.LoadGameScene);
        SubScene[] subScenes = FindObjectsByType<SubScene>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (serverWorld != null)
            await LoadSubScenesAsync(subScenes, serverWorld);
        if (clientWorld != null)
            await LoadSubScenesAsync(subScenes, clientWorld);

        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.LoadingDone);
        Debug.Log("ConnectionHandlerNew: Finished loading worlds and subscenes.");
        return sessionTransport;
        //return null;
    }

    private void CreateEntityWorlds(ISession session, out World serverWorld, out World clientWorld)
    {
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.CreateWorld);
        DestroySimulationWorld();

        serverWorld = null;
        clientWorld = null;
        switch (ClientServerBootstrap.RequestedPlayType)
        {
            case ClientServerBootstrap.PlayType.ClientAndServer:
                //role = NetworkRole.Host;
                clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
                ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                Debug.Log($"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                break;
            case ClientServerBootstrap.PlayType.Server:
                //role = NetworkRole.Server;
                serverWorld = ClientTransportHelper.ServerWorld;
                ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                Debug.Log($"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                break;
            case ClientServerBootstrap.PlayType.Client:
                //role = NetworkRole.Client;
                clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                Debug.Log($"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                break;
            default:
                Debug.LogError("ConnectionHandlerNew: No valid role specified.");
                break;
        }
        //if (session.IsHost)
        //{
        //    serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        //}
    }

    private void CreateEntityWorlds(out World serverWorld, out World clientWorld)
    {
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.CreateWorld);
        DestroySimulationWorld();

        serverWorld = null;
        clientWorld = null;
        switch (ClientServerBootstrap.RequestedPlayType)
        {
            case ClientServerBootstrap.PlayType.ClientAndServer:
                //role = NetworkRole.Host;
                clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
                ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                Debug.Log($"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                break;
            case ClientServerBootstrap.PlayType.Server:
                //role = NetworkRole.Server;
                serverWorld = ClientTransportHelper.ServerWorld;
                ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                Debug.Log($"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                break;
            case ClientServerBootstrap.PlayType.Client:
                //role = NetworkRole.Client;
                clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                Debug.Log($"Connection Request Type {ClientServerBootstrap.RequestedPlayType}");
                //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                break;
            default:
                Debug.LogError("ConnectionHandlerNew: No valid role specified.");
                break;
        }
    }

    static async Task LoadSceneAsync(string sceneName, SessionData.LoadingSteps step)
    {
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
            return;
        var sceneLoading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        UpdateLoadingStateAsync(step, sceneLoading);
        await sceneLoading;
    }
    static async void UpdateLoadingStateAsync(SessionData.LoadingSteps step, AsyncOperation loadingTask)
    {
        while (loadingTask != null && !loadingTask.isDone)
        {
            SessionData.Instance.UpdateLoading(step, loadingTask.progress);
            await Awaitable.NextFrameAsync();
        }
    }

    static async Task LoadGameplayScenesAsync()
    {
        await LoadSceneAsync("MultiplayerTest", SessionData.LoadingSteps.LoadGameScene);
    }


    public static async Task LoadGameplayAsync(World server, World client)
    {
        await LoadGameplayScenesAsync();
        if (server != null)
            await WaitForAllSubScenesToLoadAsync(server, SessionData.LoadingSteps.LoadServer);
        if (client != null)
            await WaitForAllSubScenesToLoadAsync(client, SessionData.LoadingSteps.LoadClient);
    }

    static async Task WaitForAllSubScenesToLoadAsync(World world, SessionData.LoadingSteps step)
    {
        if (world == null)
            return;

        SessionData.Instance.UpdateLoading(step);

        using var scenesQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SceneReference>());
        using var scenesLeftToLoad = scenesQuery.ToEntityListAsync(Allocator.Persistent, out var handle);
        handle.Complete();

        float count = scenesLeftToLoad.Length;
        while (scenesLeftToLoad.Length > 0)
        {
            for (var i = 0; i < scenesLeftToLoad.Length; i++)
            {
                var sceneEntity = scenesLeftToLoad[i];
                if (SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
                {
                    scenesLeftToLoad.RemoveAt(i);
                    var numLoaded = count - scenesLeftToLoad.Length;
                    var loadingProgress = numLoaded / count;
                    SessionData.Instance.UpdateLoading(step, loadingProgress);
                    i--;
                }
            }

            await Awaitable.NextFrameAsync();
        }
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
