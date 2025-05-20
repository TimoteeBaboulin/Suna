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
        foreach (var (client, choiceTeam, clientEntity) in
        SystemAPI.Query<ClientComponent, RefRO<TeamChoiceComponent>>()
        .WithEntityAccess())
        {
            if (!SystemAPI.HasComponent<GhostOwnerIsLocal>(clientEntity))
                continue;

            //TODO: Handle changing team by removing the existing entities and creating a new one
            //TODO: Potentially link this to the isRelease variable (check with Game Manager first)
            //Uncomment this if you don't want to be able to change team
            //if (client.team != TeamSideType.Neutre)
            //    continue;

            EntityQuery query = new EntityQueryBuilder(allocator: Allocator.Temp).WithAll<ClientComponent, GhostOwnerIsLocal>().Build(ref state);

            ChooseTeamRpc rpc = new ChooseTeamRpc
            {
                clientEntity = query.ToEntityArray(Allocator.Temp)[0]
            };
            rpc.team = choiceTeam.ValueRO.team;
            RpcUtils.SendClientToServerRpc(ref rpc);

            state.EntityManager.RemoveComponent<TeamChoiceComponent>(clientEntity);
        }
    }
}
