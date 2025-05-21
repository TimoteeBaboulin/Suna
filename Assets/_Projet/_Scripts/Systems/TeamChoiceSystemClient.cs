using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

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
            Debug.Log($"Team choice component added");
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (client, choiceTeam, clientEntity) in
        SystemAPI.Query<ClientComponent, RefRO<TeamChoiceComponent>>()
        .WithAll<GhostOwnerIsLocal>()
        .WithEntityAccess())
        {
            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                ChooseTeamRpc rpc = new ChooseTeamRpc
                {
                    clientEntity = clientEntity, /*query.ToEntityArray(Allocator.Temp)[0]*/
                    team = TeamSideType.Corpo
                };

                RpcUtils.SendClientToServerRpc(ref rpc);
            }
            else if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                ChooseTeamRpc rpc = new ChooseTeamRpc
                {
                    clientEntity = clientEntity, /*query.ToEntityArray(Allocator.Temp)[0]*/
                    team = TeamSideType.Natif
                };

                RpcUtils.SendClientToServerRpc(ref rpc);
            }
        }

            foreach (var (client, choiceTeam, clientEntity) in
        SystemAPI.Query<ClientComponent, RefRO<TeamChoiceComponent>>()
        .WithAll<GhostOwnerIsLocal>()
        .WithEntityAccess())
        {
            //if (!SystemAPI.HasComponent<GhostOwnerIsLocal>(clientEntity))
            //    continue;

            //TODO: Handle changing team by removing the existing entities and creating a new one
            //TODO: Potentially link this to the isRelease variable (check with Game Manager first)
            //Uncomment this if you don't want to be able to change team
            //if (client.team != TeamSideType.Neutre)
            //    continue;

            //EntityQuery query = new EntityQueryBuilder(allocator: Allocator.Temp).WithAll<ClientComponent, GhostOwnerIsLocal>().Build(ref state);

            ChooseTeamRpc rpc = new ChooseTeamRpc
            {
                clientEntity = clientEntity, /*query.ToEntityArray(Allocator.Temp)[0]*/
                team = choiceTeam.ValueRO.team
            };
            //rpc.team = choiceTeam.ValueRO.team;

            RpcUtils.SendClientToServerRpc(ref rpc);

            Debug.Log($"Team choice RPC send {choiceTeam.ValueRO.team}");

            ecb.RemoveComponent<TeamChoiceComponent>(clientEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
