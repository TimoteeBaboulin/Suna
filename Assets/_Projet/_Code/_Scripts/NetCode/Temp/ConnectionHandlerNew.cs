using GameNetwork;
using GameNetwork.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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


public class ConnectionHandlerNew : MonoBehaviour
{
    private ushort _port = 7979;
    private string _ip;
    private string _localIp = "127.0.0.1";
    private bool isClientLocal;
    private ConnectionSettings connectionSettings;

    private ClientTransportHelper sessionTransport = null;
    public string IP { get; private set; }
    public ushort Port { get; private set; }
    public bool ClientLocal => isClientLocal;
    protected void Awake()
    {
        connectionSettings = GetComponent<ConnectionSettings>();
        isClientLocal = connectionSettings.isClientLocal;
        _ip = connectionSettings.IP;
        IP = (RequestedPlayType == PlayType.ClientAndServer || isClientLocal) ? _localIp : _ip;
        Port =  connectionSettings.Port;
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
        Debug.Log($"ConnectionHandlerNew: Role is {RequestedPlayType}");
    }


    public async Task<ClientTransportHelper> ConnectToSessionAsync(CancellationToken token, string sessionID)
    {
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.StartLoading);
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.InitializeConnection);

        LoadUtils.CreateEntityWorlds(out var serverWorld, out var clientWorld);

        sessionTransport = await new ClientTransportHelper(IP, Port, isClientLocal).CreateOrJoinSessionAsync(sessionID, token);
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

        await LoadUtils.LoadGameplayAsync(serverWorld, clientWorld);

        await LoadUtils.LoadSceneAsync("MultiplayerTest", SessionData.LoadingSteps.LoadGameScene);
        if (clientWorld != null)
        {
            await WaitForGhostReplicationAsync(clientWorld);
            //await WaitForAttachedCameraAsync(clientWorld);
        }

        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.LoadingDone);
        return sessionTransport;
    }

    private async Task WaitForGhostReplicationAsync(World world, CancellationToken cancellationToken = default)
    {
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.WorldReplication);
        using var ghostCountQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GhostCount>());
        var waitedForTicks = 0;
        while (true)
        {
            if (ghostCountQuery.TryGetSingleton<GhostCount>(out var ghostCount))
            {
                var synchronizingPercentage = ghostCount.GhostCountOnServer == 0
                    ? math.saturate(ghostCount.GhostCountInstantiatedOnClient / (float)ghostCount.GhostCountOnServer)
                    : waitedForTicks > 60 ? 1f : 0f; 

                SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.WorldReplication, synchronizingPercentage);
                if (synchronizingPercentage > 0.99f) 
                    return;
            }
            await Awaitable.NextFrameAsync(cancellationToken);
            waitedForTicks++;
        }
    }

    private async Task WaitForAttachedCameraAsync(World world, CancellationToken cancellationToken = default)
    {
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.WaitingOnPlayer);
        using var mainEntityCameraQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CharacterLocalViewRotation>());
        while (!mainEntityCameraQuery.HasSingleton<CharacterLocalViewRotation>())
        {
            await Awaitable.NextFrameAsync(cancellationToken);
        }
        await Awaitable.NextFrameAsync(cancellationToken);
    }
}
