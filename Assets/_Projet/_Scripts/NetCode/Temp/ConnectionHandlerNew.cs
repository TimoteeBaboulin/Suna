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
using Unity.VisualScripting;
using Unity_NetCode_Generated_Unity_Transforms;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
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
        ClientTransportHelper.CurrentPort = (ushort)connectionSettings.Port;
    }

    public async Task<ClientTransportHelper> Connect(CancellationToken token)
    {
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.StartLoading);
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.InitializeConnection);

        LoadUtils.CreateEntityWorlds();
        if (RequestedPlayType == PlayType.ClientAndServer)
        {
            sessionTransport = await ServerSessionFactory.CreateServerSession(ClientTransportHelper.CurrentIP, ClientTransportHelper.CurrentPort);
        }
        else
        {
            await QuerySessionsAsync();

            var listOfJoinedSession = await MultiplayerService.Instance.GetJoinedSessionIdsAsync();

            if (listOfJoinedSession.Count > 0)
            {
                foreach (var joinedSessionID in listOfJoinedSession)
                {
                    sessionTransport = await new ClientTransportHelper().ReconnectByIdAsync(joinedSessionID, token);
                    Debug.Log($"[WaitUntilSessionIsFullAsync] Debug log {sessionTransport.Session.AvailableSlots}");
                    await WaitUntilSessionIsFullAsync(ClientTransportHelper.instance.Session, token);
                    if (ClientTransportHelper.instance.Session.Id == joinedSessionID)
                    {
                        Debug.Log($"found session already joined{joinedSessionID}");
                        break;
                    }
                }
            }
            else
            {
                sessionTransport = await new ClientTransportHelper().JoinSessionByIdAsync(sessionID, token);
            }
        }

        if (connectionSettings.isRelease)
        {
            Debug.Log($"[WaitUntilSessionIsFullAsync] Debug log {sessionTransport.Session.AvailableSlots}");
            await WaitUntilSessionIsFullAsync(sessionTransport.Session, token);
        }

        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.WaitingConnection);
        await LoadUtils.LoadGameplayAsync(ClientTransportHelper.ServerWorld, ClientTransportHelper.ClientWorld);
        await LoadUtils.LoadSceneAsync((int)connectionSettings.sceneToLoad, SessionData.LoadingSteps.LoadGameScene);
        if (ClientTransportHelper.ServerWorld != null)
        {
            using var drvQuery = ClientTransportHelper.ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());

            if (drvQuery.TryGetSingletonRW<NetworkStreamDriver>(out var serverDriver))
            {
                serverDriver.ValueRW.Listen(sessionTransport.ListenEndpoint);
            }
            else
            {
                Debug.LogError("NetworkStreamDriver entity not found. Ensure the subscene is loaded and instantiated correctly.");
            }
        }

        if (ClientTransportHelper.ClientWorld != null)
        {
            using var drvQuery = ClientTransportHelper.ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());

            if (drvQuery.TryGetSingletonRW<NetworkStreamDriver>(out var clientDriver))
            {
                clientDriver.ValueRW.Connect(ClientTransportHelper.ClientWorld.EntityManager, sessionTransport.ConnectEndpoint);
            }
            else
            {
                Debug.LogError("NetworkStreamDriver entity not found. Ensure the subscene is loaded and instantiated correctly.");
            }
        }

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

    private async Task WaitUntilSessionIsFullAsync(ISession session, CancellationToken token)
    {
        while (!token.IsCancellationRequested && session.AvailableSlots > 0)
        {
            try
            {
                SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.WaitingForPlayers);
                await Task.Delay(TimeSpan.FromMilliseconds(500), token);
            }
            catch (Exception)
            {
               //No need to catch the exception here
            }
        }
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
        using var mainEntityCameraQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CharacterViewRotation>());
        while (!mainEntityCameraQuery.HasSingleton<CharacterViewRotation>())
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
            Debug.Log("No sessions found.");
            return;
        }

        Debug.Log($"TOTAL Session count : {results.Sessions.Count}.");
        foreach (var session in results.Sessions)
        {
            Debug.Log(session.Name);
            if (session.Name == ClientTransportHelper.CurrentPort.ToString())
            {
                Debug.Log($"Session match found {session.Name}.");
                sessionID = session.Id;
                break;
            }
        }
    }
}
