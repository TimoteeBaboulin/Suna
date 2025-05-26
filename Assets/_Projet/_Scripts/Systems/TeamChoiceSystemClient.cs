using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct TeamChoiceComponent : IComponentData
{
    public TeamSideType team;
}

 public struct MessageToServer : IRpcCommand
{
    public FixedString32Bytes word;
}


[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct TeamChoiceSystemClient : ISystem
{
    static public void SendTeamChoice(EntityManager entityManager, TeamSideType team)
    {
        EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ClientComponent, GhostOwnerIsLocal>()
            .WithNone<TeamChoiceComponent>().Build(entityManager);
        foreach (var entity in entityQuery.ToEntityArray(Allocator.Temp))
        {
            entityManager.AddComponentData(entity, new TeamChoiceComponent { team = team });
        }
        
    }
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (client, choiceTeam, clientEntity) in
        SystemAPI.Query<ClientComponent, RefRO<TeamChoiceComponent>>()
        .WithEntityAccess())
        {
            if (!SystemAPI.HasComponent<GhostOwnerIsLocal>(clientEntity))
                continue;

            //TODO: Potentially link this to the isRelease variable (check with Game Manager first)
            //Uncomment this if you don't want to be able to change team
            //if (client.team != TeamSideType.Neutre)
            //    continue;

            Entity correctEntity = Entity.Null;
            EntityQuery query = new EntityQueryBuilder(allocator: Allocator.Temp).WithAll<ClientComponent, GhostOwnerIsLocal>().Build(ref state);
            var clients = query.ToEntityArray(Allocator.Temp);
            var clientComponents = query.ToComponentDataArray<ClientComponent>(Allocator.Temp);

            for (int i = 0; i < clients.Length; i++)
            {
                Debug.Log($"Checking entity {clients[i]} with playerID: {clientComponents[i].playerID}");

                if (clientComponents[i].playerID.Equals(client.playerID))
                {
                    Debug.Log($"Match found! Entity: {clients[i]} matches playerID: {client.playerID}");
                    correctEntity = clients[i];
                    break;
                }
            }

            if (correctEntity == Entity.Null)
            {
                continue;
            }

            if (!SystemAPI.HasComponent<GhostOwner>(correctEntity))
            {
                Debug.LogWarning("No GhostOwner component found on the correct entity.");
                continue;
            }

            var ghostOwner = SystemAPI.GetComponent<GhostOwner>(correctEntity);
            var networkID = ghostOwner.NetworkId;  

            ChooseTeamRpc rpc = new ChooseTeamRpc
            {
                networkID = networkID,  
                team = choiceTeam.ValueRO.team
            };

            RpcUtils.SendClientToServerRpc(ref rpc);
            ecb.RemoveComponent<TeamChoiceComponent>(clientEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
