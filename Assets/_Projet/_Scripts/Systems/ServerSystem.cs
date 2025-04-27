using GameNetwork.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using Unity.Transforms;
using UnityEngine;
using static PlayerHelpers;

public struct ServerMessageRpcCommand : IRpcCommand
{
    public FixedString64Bytes message;
}

[GhostComponent]
public struct ClientComponent : IComponentData
{
    [GhostField]
    public int networkID;
    [GhostField]
    public FixedString64Bytes playerID;
    [GhostField]
    public TeamSideType team;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ServerSystem : SystemBase
{
    private ComponentLookup<NetworkId> _clients;

    protected override void OnCreate()
    {
        _clients = GetComponentLookup<NetworkId>(true);

        RequireForUpdate<NetworkId>();

    }
    protected override void OnUpdate()
    {
        _clients.Update(this);

        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ClientMessageRpcCommand>>().WithEntityAccess())
        {
            ServerConsole.Log(ServerConsole.LogType.Info, $"{command.ValueRO.message} from client index {request.ValueRO.SourceConnection.Index}, version {request.ValueRO.SourceConnection.Version}");
            Debug.Log($"{command.ValueRO.message} from client index {request.ValueRO.SourceConnection.Index}");
            commandBuffer.DestroyEntity(entity);
        }

        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<ClientComponent>().WithEntityAccess())
        {
            InstantiateClient(entity, commandBuffer);
        }

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();

        Dependency.Complete();
    }

    #region Public Methods

    public void InstantiateClient(Entity ownerEntity, EntityCommandBuffer ecb)
    {
        if (SystemAPI.TryGetSingleton(out ClientPrefabData prefabManager))
        {
            if (prefabManager.Client == null)
            {
                return;
            }

            NetworkId networkId = SystemAPI.GetComponent<NetworkId>(ownerEntity);
            FixedString128Bytes worldName = ClientServerBootstrap.ServerWorld.Name;

            Entity client = ecb.Instantiate(prefabManager.Client);

            ecb.SetComponent(client, new GhostOwner()
            {
                NetworkId = networkId.Value
            });
            ecb.AppendToBuffer(ownerEntity, new LinkedEntityGroup() { Value = client });
            var hostSession = ClientTransportHelper.instance.Session as IServerSession;

            IPlayer currentPlayer = PlayerHelpers.FindCurrentPlayerForNetworkId(networkId.Value);
            if (currentPlayer == null)
                return;

            TeamSideType assignedTeam = AssignTeamToPlayer(currentPlayer);
            Debug.Log($"[OnPlayerJoined] Player with id {currentPlayer.Id} created.");
            hostSession.SavePlayerDataAsync(currentPlayer.Id);

            //Do not remove this code, it's not nice, not ugly but it's fucks hard and if you remove it, it will fuck you
            ecb.AddComponent(ownerEntity, new ClientComponent
            {
                networkID = networkId.Value,
                playerID = currentPlayer.Id,
                team = assignedTeam
            });

            ecb.SetComponent(client, new ClientComponent
            {
                networkID = networkId.Value,
                playerID = currentPlayer.Id,
                team = assignedTeam
            });

            ServerConsole.Log(ServerConsole.LogType.Info, $"New Client : " +
                $"NetworkId {networkId.Value} " +
                $"currentPlayerID {currentPlayer.Id} " +
                $"team {GetPlayerInTeam(networkId.Value)} " +
                $"world {worldName}");
        }
    }
    #endregion
}


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateAfter(typeof(CountPlayersSystemServer))]
public partial class SessionStatusSystem : SystemBase
{
    private float logInterval = 5.0f;
    private float timer;
    private bool didSubscribe = false;
    protected override void OnCreate()
    {
        var clients = GetComponentLookup<NetworkId>(true);

        RequireForUpdate<NetworkId>();
#if UNITY_SERVER
        Debug.Log("[SessionStatusSystem] Waiting for session to be created...");
#endif
    }

    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        timer += deltaTime;

        if (timer >= logInterval)
        {
            timer = 0f;


#if UNITY_SERVER
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Session ID: {ClientTransportHelper.instance.Session.Id}");
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Session Name: {ClientTransportHelper.instance.Session.Name}");
#endif
            if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
            {
#if UNITY_SERVER
                Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Current Nb of player: {ClientTransportHelper.instance.Session.PlayerCount - 1}");//Minus the server, as it counts as player
#endif
            }
            else
            {
#if UNITY_SERVER
                Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Current Nb of player: {ClientTransportHelper.instance.Session.PlayerCount}");
#endif
            }
#if UNITY_SERVER
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Session State: {ClientTransportHelper.instance.Session.State} "); ;
#endif
            PlayerHelpers.AliveCounts currentCounts = PlayerHelpers.GetCurrentAliveCounts(World);
#if UNITY_SERVER
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated native players alive: {currentCounts.natifPlayersAlive}");
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated corpo players alive: {currentCounts.corpoPlayersAlive}");
#endif
            PlayerHelpers.GlobalTeamCount teamCounts = PlayerHelpers.GetCurrentTeamCounts();
#if UNITY_SERVER
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated native teamCounts: {teamCounts.natifPlayersCount}");
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated corpo teamCounts: {teamCounts.corpoPlayersCount}");

            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated native teamCounts in Session property: " +
                $"{ClientTransportHelper.instance.Session.AsHost().Properties["CountTeamNatif"].Value}");
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated corpo teamCounts in Session property: " +
                $"{ClientTransportHelper.instance.Session.AsHost().Properties["CountTeamCorpo"].Value}");
#endif

            //PlayerHelpers.TeamList teamList = PlayerHelpers.GetTeamList();
            //Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated native teamList: {teamList.natifPlayers.Count}");
            //Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated corpo teamList: {teamList.corpoPlayers.Count}");

        }
    }
}