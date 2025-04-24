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
public struct ClientComponent : IComponentData
{
    public int networkID;
    public FixedString64Bytes playerID;
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
            var hostSession = ClientTransportHelper.instance.Session.AsHost();

            IPlayer currentPlayer = PlayerHelpers.FindCurrentPlayerForNetworkId(networkId.Value);
            string teamString = AssignTeamToPlayer(currentPlayer);
            Debug.Log($"[OnPlayerJoined] Player with id {currentPlayer.Id} created.");
            if (currentPlayer != null)
            {
                currentPlayer.SetProperty("team", new PlayerProperty(teamString, VisibilityPropertyOptions.Public));
                hostSession.SavePlayerDataAsync(currentPlayer.Id);
            }

            TeamSideType assignedTeam = TeamSideType.Neutre;
            switch (teamString)
            {
                case "Corpo":
                    assignedTeam = TeamSideType.Corpo;
                    break;
                case "Natif":
                    assignedTeam = TeamSideType.Natif;
                    break;
                default:
                    assignedTeam = TeamSideType.Neutre;
                    break;
            }


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
                $"team {assignedTeam} " +
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
        Debug.Log("[SessionStatusSystem] Waiting for session to be created...");
    }

    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        timer += deltaTime;

        if (timer >= logInterval)
        {
            timer = 0f;

        
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Session ID: {ClientTransportHelper.instance.Session.Id}");
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Session Name: {ClientTransportHelper.instance.Session.Name}");

            if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
            {
                Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Current Nb of player: {ClientTransportHelper.instance.Session.PlayerCount - 1}");//Minus the server, as it counts as player
            }
            else
            {
                Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Current Nb of player: {ClientTransportHelper.instance.Session.PlayerCount}");
            }
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Session State: {ClientTransportHelper.instance.Session.State} "); ;

            PlayerHelpers.AliveCounts currentCounts = PlayerHelpers.GetCurrentAliveCounts(World);
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated native players alive: {currentCounts.natifPlayersAlive}");
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated corpo players alive: {currentCounts.corpoPlayersAlive}");

            PlayerHelpers.GlobalTeamCount teamCounts = PlayerHelpers.GetCurrentTeamCounts();
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated native teamCounts: {teamCounts.natifPlayersCount}");
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated corpo teamCounts: {teamCounts.corpoPlayersCount}");


            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated native teamCounts in Session property: " +
                $"{ClientTransportHelper.instance.Session.AsHost().Properties["CountTeamNatif"].Value}");
            Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated corpo teamCounts in Session property: " +
                $"{ClientTransportHelper.instance.Session.AsHost().Properties["CountTeamCorpo"].Value}");
            //PlayerHelpers.TeamList teamList = PlayerHelpers.GetTeamList();
            //Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated native teamList: {teamList.natifPlayers.Count}");
            //Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Calculated corpo teamList: {teamList.corpoPlayers.Count}");
        }
    }
}