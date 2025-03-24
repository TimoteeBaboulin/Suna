using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static RoundSystemServer;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct HarvesterSystemServer : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<HarvesterComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        #region NewRound
        bool newRound = false;

        foreach (var (newRoundTag, entity) in SystemAPI.Query<NewRoundTag>().WithEntityAccess())
        {
            ecb.RemoveComponent<NewRoundTag>(entity);
            newRound = true;
        }

        Debug.Log("Harvester Server System");

        if (newRound)
        {
            
            NativeList<Entity> corpoEntities = new NativeList<Entity>(Allocator.Temp);
            float3 corpoSpawnPosition = float3.zero;

            foreach (var (spawn, spawnEntity) in SystemAPI.Query<TeamSpawnComponent>().WithEntityAccess())
            {
                if (spawn.team == TeamSideType.Corpo)
                {
                    var buffer = SystemAPI.GetBuffer<SpawnPointBufferComponent>(spawnEntity);
                    int random = UnityEngine.Random.Range(0, buffer.Length);
                    corpoSpawnPosition = buffer[random];
                    break;
                }
            }

            foreach (var (playerComponent, clientEntity) in SystemAPI.Query<RefRW<ClientComponent>>().WithAll<WaitForRespawnTag>().WithEntityAccess())
            {
                corpoEntities.Add(clientEntity);
            }

            foreach (var (harvesterRW, harvesterEntity) in SystemAPI.Query<RefRW<HarvesterComponent>>().WithEntityAccess())
            {
                if (corpoEntities.Length > 0)
                {
                    int random = UnityEngine.Random.Range(0, corpoEntities.Length);
                    Entity characterEntity = corpoEntities[random];
                    corpoEntities.RemoveAt(random);

                    harvesterRW.ValueRW.Owner = characterEntity;
                }
                else
                {
                    harvesterRW.ValueRW.Owner = Entity.Null;
                    SystemAPI.GetComponentRW<LocalTransform>(harvesterEntity).ValueRW.Position = corpoSpawnPosition;
                }
            }
        }
        #endregion //NewRound

        EntityQueryBuilder builder = new EntityQueryBuilder(allocator: Allocator.Temp);
        EntityQuery query = builder.WithAll<HarvesterComponent>().Build(ref state);

        NativeArray<HarvesterComponent> harvesters = query.ToComponentDataArray<HarvesterComponent>(Allocator.Temp);

        //TODO: Handle plant time and message relaying
        foreach ((RefRO<ReceiveRpcCommandRequest> request, HarvesterStartPlant rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, HarvesterStartPlant>().WithEntityAccess())
        {
            //int clientId = SystemAPI.GetComponent<ClientComponent>(entity).

            //if (!harvesters.Any((obj) =>
            //{
            //    return obj.ownerNetworkId == request.ValueRO.
            //}))
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
