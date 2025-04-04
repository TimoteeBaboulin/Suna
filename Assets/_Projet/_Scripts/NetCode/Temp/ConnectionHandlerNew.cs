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
    private bool isClientLocal;
    private ConnectionSettings connectionSettings;

    private ClientTransportHelper sessionTransport = null;
    public string IP { get; private set; }
    public ushort Port { get; private set; }
    public bool ClientLocal => isClientLocal;

    private static readonly List<string> PlayerProcessingQueue = new List<string>();
    private static bool processingQueue = false;
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
        await LoadUtils.LoadGameplayAsync(serverWorld, clientWorld);
        await LoadUtils.LoadSceneAsync("MultiplayerTest", SessionData.LoadingSteps.LoadGameScene);
        await WaitUntilSessionIsFullAsync(token, clientWorld);
        //if (clientWorld != null)
        //{
        //    await WaitForGhostReplicationAsync(clientWorld);
        //    //await WaitForAttachedCameraAsync(clientWorld);
        //}

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
}
