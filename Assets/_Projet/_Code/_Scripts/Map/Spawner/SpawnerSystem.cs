using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public struct WaitForRespawnTag : IComponentData { }
public struct ResetStuffTag : IComponentData { }

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
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
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct RespawnSystem : ISystem
{
    ComponentLookup<LocalTransform> respawnPtLookupInit;
    ComponentLookup<ResetStuffTag> resetStuffLookupInit;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<PlayerComponent, WaitForRespawnTag>();
        state.RequireForUpdate(state.GetEntityQuery(builder));

        respawnPtLookupInit = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        resetStuffLookupInit = state.GetComponentLookup<ResetStuffTag>(isReadOnly: true);
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (player, entity) in SystemAPI.Query<RefRW<PlayerComponent>>().WithAll<WaitForRespawnTag>().WithEntityAccess())
        {
            SpawnerComponent spawnerComponent;

            if (SystemAPI.TryGetSingleton(out spawnerComponent))
            {
                
                Entity spawnerEntity = SystemAPI.GetSingletonEntity<SpawnerComponent>();
                LocalTransform respawnZoneTransform = state.EntityManager.GetComponentData<LocalTransform>(spawnerEntity);

                

                //transform.ValueRW.Position = respawnZoneTransform.Position;
                //hp.ValueRW.Value = maxHp.ValueRO.Value;
                int networkId = state.EntityManager.GetComponentData<GhostOwner>(entity).NetworkId;
                SpawnCharacter(entity, networkId, ecb, respawnZoneTransform.Position);
               

                ecb.RemoveComponent<WaitForRespawnTag>(entity);
            }
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        //if (resetStuffLookup.HasComponent(spawnerEntity))
        //{
        //    //TODO : Vider l'inventaire
        //    commandBuffer.RemoveComponent<ResetStuffTag>(playerEntity.Index, playerEntity);
        //}
    }

    public void SpawnCharacter(Entity ownerEntity, int networkId, EntityCommandBuffer ecb, float3 position)
    {
        PrefabsData prefabManager = SystemAPI.GetSingleton<PrefabsData>();
        
        if (prefabManager.character == null)
        {
            return;
        }

        FixedString128Bytes worldName = ConnectionManager.Instance.Server.Name;

        Entity player = ecb.Instantiate(prefabManager.character);
        ecb.SetComponent(player, new LocalTransform() //Set position
        {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1.0f
        });
        ecb.SetComponent(player, new GhostOwner() //Set owner of player to connection
        {
            NetworkId = networkId
        });
        ecb.AppendToBuffer(ownerEntity, new LinkedEntityGroup() //Link it to connection
        {
            Value = player
        });

        //ServerConsole.Log(ServerConsole.LogType.Info, $"Player spawned with NetworkId {networkId}, in the world {worldName}");
    }
}




























//    SpawnerComponent spawnerComponent;
//    if (SystemAPI.TryGetSingleton(out spawnerComponent))
//    {
//        Debug.Log("HHHHHHHHHHHHHHHHHHHHHHHHHHHHH");
//        respawnPtLookupInit.Update(ref state);
//        resetStuffLookupInit.Update(ref state);

//        var ecb = new EntityCommandBuffer(Allocator.TempJob);

//        RespawnJob job = new RespawnJob
//        {
//            dt = SystemAPI.Time.DeltaTime,
//            networkTime = SystemAPI.GetSingleton<NetworkTime>(),
//            commandBuffer = ecb.AsParallelWriter(),

//            respawnPtLookup = respawnPtLookupInit,
//            resetStuffLookup = resetStuffLookupInit,

//            spawnerEntity = SystemAPI.GetSingletonEntity<SpawnerComponent>()
//        };
//        state.Dependency = job.ScheduleParallel(state.Dependency);

//        state.Dependency.Complete();
//        ecb.Playback(state.EntityManager);
//        ecb.Dispose();
//    }
//}
//}

//[BurstCompile]
//public partial struct RespawnJob : IJobEntity
//{
//    public float dt;
//    public NetworkTime networkTime;
//    public EntityCommandBuffer.ParallelWriter commandBuffer;
//    public Entity spawnerEntity;

//    [ReadOnly] public ComponentLookup<LocalTransform> respawnPtLookup;
//    [ReadOnly] public ComponentLookup<ResetStuffTag> resetStuffLookup;

//    public void Execute(Entity playerEntity, in CharacterControllerComponent controler,
//        ref CurrentHealthComponent hp, in MaxHealthComponent maxHp, RefRW<LocalTransform> playerTransform)
//    {
//        //LocalTransform playerTransform;
//        //respawnPtLookup.TryGetComponent(playerEntity, out playerTransform);

//        LocalTransform respawnZoneTransform;
//        respawnPtLookup.TryGetComponent(spawnerEntity, out respawnZoneTransform);

//        //Changement de position, récupération des PV
//        playerTransform.ValueRW.Position = respawnZoneTransform.Position;
//        hp.Value = maxHp.Value;

//        if (resetStuffLookup.HasComponent(spawnerEntity))
//        {
//            //TODO : Vider l'inventaire
//            commandBuffer.RemoveComponent<ResetStuffTag>(playerEntity.Index, playerEntity);
//        }
//    }
//}

