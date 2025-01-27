using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;

public struct WaitForRespawnTag : IComponentData { }
public struct ResetStuffTag : IComponentData { }

public struct RespawnPoints : IBufferElementData
{
    public Entity entity;
}

[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct OnDieSystem : ISystem
{
    [ReadOnly]
    public ComponentLookup<ResetStuffTag> resetStuffLookupInit;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<CurrentHealthComponent>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
        resetStuffLookupInit = state.GetComponentLookup<ResetStuffTag>(isReadOnly: true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        resetStuffLookupInit.Update(ref state);

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        OnDieJob job = new OnDieJob
        {
            dt = SystemAPI.Time.DeltaTime,
            networkTime = SystemAPI.GetSingleton<NetworkTime>(),
            commandBuffer = ecb.AsParallelWriter(),
            resetStuffLookup = resetStuffLookupInit
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);

        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct OnDieJob : IJobEntity
{
    public float dt;
    public NetworkTime networkTime;
    public EntityCommandBuffer.ParallelWriter commandBuffer;

    [ReadOnly]
    public ComponentLookup<ResetStuffTag> resetStuffLookup;

    public void Execute(Entity entity, in CurrentHealthComponent hp)
    {
        if (!resetStuffLookup.HasComponent(entity))
        {
            if (hp.Value <= 0)
            {
                //commandBuffer.AddComponent<WaitForRespawnTag>(entity.Index, entity);
                commandBuffer.AddComponent<ResetStuffTag>(entity.Index, entity);
            }
        }
    }
}


// ///////////////////////////////////////////////////////////////////////////////////////////////////////////


[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct RespawnSystem : ISystem
{
    BufferLookup<RespawnPoints> respawnPointsLookupInit;
    ComponentLookup<LocalTransform> respawnPtLookupInit;
    ComponentLookup<ResetStuffTag> resetStuffLookupInit;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<WaitForRespawnTag>();
        state.RequireForUpdate(state.GetEntityQuery(builder));

        respawnPointsLookupInit = state.GetBufferLookup<RespawnPoints>(isReadOnly: true);
        respawnPtLookupInit = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        resetStuffLookupInit = state.GetComponentLookup<ResetStuffTag>(isReadOnly: true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        respawnPointsLookupInit.Update(ref state);
        respawnPtLookupInit.Update(ref state);
        resetStuffLookupInit.Update(ref state);

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        RespawnJob job = new RespawnJob
        {
            dt = SystemAPI.Time.DeltaTime,
            networkTime = SystemAPI.GetSingleton<NetworkTime>(),
            commandBuffer = ecb.AsParallelWriter(),

            respawnPointsLookup = respawnPointsLookupInit,
            respawnPtLookup = respawnPtLookupInit,
            resetStuffLookup = resetStuffLookupInit
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);

        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[BurstCompile]
public partial struct RespawnJob : IJobEntity
{
    public float dt;
    public NetworkTime networkTime;
    public EntityCommandBuffer.ParallelWriter commandBuffer;

    [ReadOnly] public BufferLookup<RespawnPoints> respawnPointsLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> respawnPtLookup;
    [ReadOnly] public ComponentLookup<ResetStuffTag> resetStuffLookup;

    public void Execute(Entity entity, in CharacterControllerComponent controler,
        ref CurrentHealthComponent hp, in MaxHealthComponent maxHp)
    {
        ////On verifie si des entitées avec le composant "RespawnPoint" exist
        if (respawnPointsLookup.HasBuffer(controler.teamEntity))
        {
            //On récupére la liste des points de respawn
            DynamicBuffer<RespawnPoints> respawnZonesBuffer;
            respawnPointsLookup.TryGetBuffer(controler.teamEntity, out respawnZonesBuffer);

            //On verifie que le nombre de point de respawn est > 0
            if (respawnZonesBuffer.Length > 0)
            {
                //On recupere le premier point de spawn de la liste 
                Entity respawnZoneEntity = respawnZonesBuffer[0].entity;

                LocalTransform playerTransform;
                respawnPtLookup.TryGetComponent(entity, out playerTransform);

                LocalTransform respawnZoneTransform;
                respawnPtLookup.TryGetComponent(respawnZoneEntity, out respawnZoneTransform);

                //Changement de position, récupération des PV
                playerTransform.Position = respawnZoneTransform.Position;
                hp.Value = maxHp.Value;

                if (resetStuffLookup.HasComponent(respawnZoneEntity))
                {
                    //TODO : Vider l'inventaire

                    commandBuffer.RemoveComponent<ResetStuffTag>(entity.Index, entity);
                }
            }
            else
                Debug.LogWarning($"No respawn zones found for team entity {controler.teamEntity}.");
        }
        else
            Debug.LogWarning($"Team entity {controler.teamEntity} does not have a RespawnZones component.");
    }
}

