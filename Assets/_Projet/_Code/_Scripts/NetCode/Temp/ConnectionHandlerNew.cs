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
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
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
    private CreateTicketResponse ticketResponse;
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
        Port = connectionSettings.Port;
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

        // sessionTransport = await new ClientTransportHelper(IP, Port, isClientLocal).CreateOrJoinSessionAsync(sessionID, token);
        sessionTransport = await new ClientTransportHelper(IP, Port, isClientLocal).JoinOrCreateMatchmakerGameAsync(token);
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

        // Set options for matchmaking
        var options = new CreateTicketOptions(
          ClientTransportHelper.GlobalQueueName,
          new Dictionary<string, object>());

        var players = new List<Unity.Services.Matchmaker.Models.Player>
{
    new ("Player" + sessionTransport.Session.CurrentPlayer.Id, new Dictionary<string, object>())
};

        ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
        Debug.Log(ticketResponse.Id);
        await ProcessTicketAsync(clientWorld);
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

    public async Task ProcessTicketAsync(World clientWorld)
    {
        bool validResult = false;

        while (!validResult)
        {
            var ticketStatus = await MatchmakerService.Instance.GetTicketAsync(ticketResponse.Id);

            if (ticketStatus == null)
            {
                await Task.Delay(1100);
                continue;
            }

            if (ticketStatus.Type == typeof(MultiplayAssignment))
            {
                MultiplayAssignment assignment = ticketStatus.Value as MultiplayAssignment;

                switch (assignment.Status)
                {
                    case MultiplayAssignment.StatusOptions.Timeout:
                        Debug.Log("Ticket Timeout");
                        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.BackToMainMenu);
                        break;
                    case MultiplayAssignment.StatusOptions.Failed:
                        Debug.Log("Failed");
                        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.BackToMainMenu);
                        break;
                    case MultiplayAssignment.StatusOptions.InProgress:
                        Debug.Log("Progress");
                        break;
                    case MultiplayAssignment.StatusOptions.Found:
                        await LoadUtils.LoadSceneAsync("MultiplayerTest", SessionData.LoadingSteps.LoadGameScene);
                        if (clientWorld != null)
                        {
                            await WaitForGhostReplicationAsync(clientWorld);
                        }
                        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.LoadingDone);
                        validResult = true;
                        break;
                    default:
                        break;
                }
            }

            await Task.Delay(1100);
        }
    }
}
