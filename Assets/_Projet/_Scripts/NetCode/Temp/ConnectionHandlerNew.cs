using GameNetwork;
using GameNetwork.Utils;
using System;
using System.Collections.Generic;
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
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using static ConnectionManager;
using static GameManager;
using static System.Collections.Specialized.BitVector32;
using static Unity.NetCode.ClientServerBootstrap;


public class ConnectionHandlerNew : MonoBehaviour
{
    private ushort _port = 7979;
    private string _ip;
    private string _localIp = "127.0.0.1";
    private ConnectionSettings connectionSettings;

    private string sessionID = null;
    private ClientTransportHelper sessionTransport = null;

    private static readonly List<string> PlayerProcessingQueue = new List<string>();
    private static bool processingQueue = false;
    protected void Awake()
    {
        connectionSettings = GetComponent<ConnectionSettings>();
        ClientTransportHelper.isClientLocal = connectionSettings.isClientLocal;
        _ip = connectionSettings.IP;
        ClientTransportHelper.CurrentIP = (RequestedPlayType == PlayType.ClientAndServer || ClientTransportHelper.isClientLocal) ? _localIp : _ip;
        ClientTransportHelper.CurrentPort = connectionSettings.Port;
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


    public async Task<ClientTransportHelper> Connect(CancellationToken token)
    {
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.StartLoading);
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.InitializeConnection);

        LoadUtils.CreateEntityWorlds();
        if (RequestedPlayType == PlayType.ClientAndServer)
        {
            sessionTransport = await ServerSessionFactory.CreateServerSession(
                ClientTransportHelper.CurrentIP,
                ClientTransportHelper.CurrentPort,
                ClientTransportHelper.isClientLocal);
        }
        else
        {
            await QuerySessionsAsync();
            sessionTransport = await new ClientTransportHelper().JoinSessionByIdAsync(sessionID, token);
        }

        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.WaitingConnection);
        if (ClientTransportHelper.ServerWorld != null)
        {
            using var drvQuery = ClientTransportHelper.ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var serverDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
            serverDriver.Listen(sessionTransport.ListenEndpoint);
        }

        if (ClientTransportHelper.ClientWorld != null)
        {
            using var drvQuery = ClientTransportHelper.ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var clientDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
            clientDriver.Connect(ClientTransportHelper.ClientWorld.EntityManager, sessionTransport.ConnectEndpoint);
        }
        await LoadUtils.LoadGameplayAsync(ClientTransportHelper.ServerWorld, ClientTransportHelper.ClientWorld);
        await LoadUtils.LoadSceneAsync((int)connectionSettings.sceneToLoad, SessionData.LoadingSteps.LoadGameScene);
        //await LoadUtils.LoadSceneAsync("MultiplayerTest", SessionData.LoadingSteps.LoadGameScene);

        //await WaitUntilSessionIsFullAsync(token, clientWorld);
        if (ClientTransportHelper.ClientWorld != null)
        {
            //await WaitForPlayerConnectionAsync(token);
            await WaitForGhostReplicationAsync(ClientTransportHelper.ClientWorld);
            //await WaitForAttachedCameraAsync(clientWorld);
        }
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.LoadingDone);
        return sessionTransport;
    }


    private async Task WaitUntilSessionIsFullAsync(CancellationToken token, World clientWorld)
    {
        string localPlayerId = sessionTransport.Session.CurrentPlayer.Id;

        lock (PlayerProcessingQueue)
        {
            PlayerProcessingQueue.Add(localPlayerId);
        }

        while (!IsSessionFull(sessionTransport.Session))
        {
            if (token.IsCancellationRequested || !Application.isPlaying)
            {
                Debug.Log("WaitUntilSessionIsFullAsync: Cancelled or application is no longer playing.");
                token.ThrowIfCancellationRequested();
                return;
            }

            Debug.Log($"GameManager: Current player count: {sessionTransport.Session.PlayerCount}, " +
                      $"available slots: {sessionTransport.Session.AvailableSlots}");

            SessionData.Instance.UpdateSessionState(
                sessionTransport.Session.PlayerCount,
                sessionTransport.Session.AvailableSlots,
                sessionTransport.Session);

            await Task.Delay(16, token);
        }

        const int baseDelayMs = 50;
        const int incrementMs = 25;

        while (true)
        {
            int index;
            lock (PlayerProcessingQueue)
            {
                index = PlayerProcessingQueue.IndexOf(localPlayerId);
            }

            if (index == 0)
            {
                break;
            }

            int dynamicDelay = baseDelayMs + (index * incrementMs);
            Debug.Log($"Waiting for my turn... My index is {index}, delaying for {dynamicDelay} ms.");
            await Task.Delay(dynamicDelay, token);
        }

        await Task.Delay(baseDelayMs, token);

        try
        {
            if (IsSessionFull(sessionTransport.Session))
            {
                if (clientWorld != null)
                {
                    await WaitForGhostReplicationAsync(clientWorld);
                    //await WaitForAttachedCameraAsync(clientWorld);
                }
            }
        }
        finally
        {
            lock (PlayerProcessingQueue)
            {
                if (PlayerProcessingQueue.Count > 0 && PlayerProcessingQueue[0] == localPlayerId)
                {
                    PlayerProcessingQueue.RemoveAt(0);
                }
            }
        }
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

    static async Task WaitForPlayerConnectionAsync(CancellationToken cancellationToken = default)
    {
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.WaitingConnection);
        // The GameManagerSystem is handling the connection/reconnection once the client world is created.
        ClientTransportHelper.State = ClientConnectionState.Connecting;
        while (ClientTransportHelper.State == ClientConnectionState.Connecting)
        {
            await Awaitable.NextFrameAsync(cancellationToken);
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
            sessionID = "0";
            Debug.Log("No sessions found.");
            return;
        }


        foreach (var session in results.Sessions)
        {
            Debug.Log(session.Name);

            if (ClientTransportHelper.isClientLocal)
            {
                sessionID = session.Id;
                break;
            }
            else
            {
                sessionID = session.Id;
                break;
            }

            //if (session.AvailableSlots != 0)
            //{
            //    SessionID = session.Id;
            //    Debug.Log($"Players: {session.AvailableSlots}/{session.MaxPlayers}");
            //    Debug.Log($"Found session ID: {session.Id}");
            //    Debug.Log($"Session code: {session.Id}");
            //}
            //else
            //{
            //    ClientSessionCreationCommand command = new ClientSessionCreationCommand() { createNewSession = true };
            //    RpcUtils.SendClientToServerRpc(ref command);

            //    Debug.Log($"Players: {session.AvailableSlots}/{session.MaxPlayers}");
            //    Debug.Log($"Found session ID: {session.Id}");
            //    Debug.Log($"Session code: {session.Id}");
            //}
        }
        //var firstSession = results.Sessions[0];
        //SessionID = firstSession.Id;
    }
}
