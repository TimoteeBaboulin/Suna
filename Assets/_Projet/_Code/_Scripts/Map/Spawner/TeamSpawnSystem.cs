using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct TeamSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WaitForRespawnTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        return;

        bool[] teamSpawnsValid = { false, false, false };
        Entity[] teamSpawnsEntities = new Entity[3];
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (spawner, entity) in SystemAPI.Query<RefRO<TeamSpawnComponent>>().WithEntityAccess())
        {
            teamSpawnsValid[(int)spawner.ValueRO.team] = true;
            teamSpawnsEntities[(int)spawner.ValueRO.team] = entity;
        }

        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        EntityQuery query = builder.WithAll<WaitForRespawnTag>().Build(ref state);

        foreach (var entity in query.ToEntityArray(Allocator.Temp))
        {
            Debug.Log(entity);
        }

        foreach (var (transform, entity) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<WaitForRespawnTag>().WithEntityAccess())
        {
            TeamSideType team = TeamSideType.Neutre;
            if (SystemAPI.HasComponent<CorpoTeamTag>(entity))
            {
                team = TeamSideType.Corpo;
            }
            else if (SystemAPI.HasComponent<NatifTeamTag>(entity))
            {
                team = TeamSideType.Natif;
            }

            if (!teamSpawnsValid[(int) team])
            {
                
                continue;
            }

            var buffer = SystemAPI.GetBuffer<SpawnPointBufferComponent>(teamSpawnsEntities[(int)team]);
            int random = Random.Range(0, buffer.Length);
            transform.ValueRW.Position = buffer[random];
            ecb.RemoveComponent<WaitForRespawnTag>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
