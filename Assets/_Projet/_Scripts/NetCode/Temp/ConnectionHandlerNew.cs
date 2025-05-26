using GameNetwork;
using GameNetwork.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
    private string _localIp = "127.0.0.1";
    private ConnectionSettings connectionSettings;

    private string sessionID = null;
    private ClientTransportHelper sessionTransport = null;
    private DateTime earliestCreationTime;

    protected void Awake()
    {
        connectionSettings = GetComponent<ConnectionSettings>();
        ClientTransportHelper.isClientLocal = connectionSettings.isClientLocal;
        ClientTransportHelper.CurrentIP = (ClientTransportHelper.isClientLocal) ? _localIp : ClientTransportHelper.GetLocalIPAddress();
        ClientTransportHelper.CurrentPort = (ushort)connectionSettings.Port;
    }

    public async Task<ClientTransportHelper> Connect(CancellationToken token)
    {
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.StartLoading);
        SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.InitializeConnection);

        LoadUtils.CreateEntityWorlds();
        if (RequestedPlayType == PlayType.ClientAndServer)
        {
            sessionTransport = await ServerSessionFactory.CreateServerSession(ClientTransportHelper.GetLocalIPAddress(), ClientTransportHelper.CurrentPort);
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
        await LoadUtils.LoadSceneAsync("MultiplayerTest", SessionData.LoadingSteps.LoadGameScene);
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

        if (ClientTransportHelper.ClientWorld != null)
        {
            await WaitForGhostReplicationAsync(ClientTransportHelper.ClientWorld);
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

        if (connectionSettings.isRelease)
        {
            if (results.Sessions.Count == 1)
            {
                var session = results.Sessions[0];
                sessionID = session.Id;
                Debug.Log($"Only one session: {session.Name} with Created Time: {session.Created}");
            }
            else
            {
                foreach (var session in results.Sessions.OrderBy(s => s.Created))
                {
                    Debug.Log($"Considering session {session.Name}, Created: {session.Created}, Slots: {session.AvailableSlots}");
                    if (session.AvailableSlots > 0)
                    {
                        sessionID = session.Id;
                        Debug.Log($"→ Selected session {session.Name} (Created: {session.Created}) and stopping search.");
                        break;
                    }
                }

                if (string.IsNullOrEmpty(sessionID))
                    Debug.Log("No session found with available slots.");
            }
        }
        else
        {
            foreach (var session in results.Sessions)
            {
                if (session.Name == connectionSettings.Port.ToString())
                {
                    sessionID = session.Id;
                    break;
                }
            }
        }
    }
}
