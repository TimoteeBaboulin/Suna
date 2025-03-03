using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public struct WaitForRespawnTag : IComponentData { }
public struct ResetStuffTag : IComponentData { }

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct OnDieSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<HasNoHealthTag>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ComponentLookup<HasNoHealthTag> hasNoHealthTagLookup = SystemAPI.GetComponentLookup<HasNoHealthTag>();
        ComponentLookup<ResetStuffTag> resetStuffLookupInit = SystemAPI.GetComponentLookup<ResetStuffTag>();

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        OnDieJob job = new OnDieJob
        {
            dt = SystemAPI.Time.DeltaTime,
            networkTime = SystemAPI.GetSingleton<NetworkTime>(),
            commandBuffer = ecb.AsParallelWriter(),
            resetStuffLookup = resetStuffLookupInit,
            HasNoHealthTagLookup = hasNoHealthTagLookup,
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(CurrentHealthComponent), typeof(Simulate))]
public partial struct OnDieJob : IJobEntity
{
    public float dt;
    public NetworkTime networkTime;
    public EntityCommandBuffer.ParallelWriter commandBuffer;

    [ReadOnly] public ComponentLookup<ResetStuffTag> resetStuffLookup;
    [ReadOnly] public ComponentLookup<HasNoHealthTag> HasNoHealthTagLookup;

    public void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, RefRO<CharacterClientAttachedComponent> CharacterPlayerAttached)
    {
        if (!resetStuffLookup.HasComponent(entity)
            && HasNoHealthTagLookup.HasComponent(entity))
        {
            commandBuffer.AddComponent<WaitForRespawnTag>(sortKey, CharacterPlayerAttached.ValueRO.Value);
            commandBuffer.DestroyEntity(sortKey, entity);

            //commandBuffer.AddComponent<ResetStuffTag>(sortKey, entity);
        }
    }
}


// ///////////////////////////////////////////////////////////////////////////////////////////////////////////


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct RespawnSystem : ISystem
{
    ComponentLookup<LocalTransform> respawnPtLookupInit;
    ComponentLookup<ResetStuffTag> resetStuffLookupInit;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<ClientComponent, WaitForRespawnTag>();
        state.RequireForUpdate(state.GetEntityQuery(builder));

        respawnPtLookupInit = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        resetStuffLookupInit = state.GetComponentLookup<ResetStuffTag>(isReadOnly: true);
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (playerComponent, entity) in SystemAPI.Query<RefRW<ClientComponent>>().WithAll<WaitForRespawnTag>().WithEntityAccess())
        {
            SpawnerComponent spawnerComponent;

            if (SystemAPI.TryGetSingleton(out spawnerComponent))
            {

                Entity spawnerEntity = SystemAPI.GetSingletonEntity<SpawnerComponent>();
                LocalTransform respawnZoneTransform = state.EntityManager.GetComponentData<LocalTransform>(spawnerEntity);

                int networkId = state.EntityManager.GetComponentData<GhostOwner>(entity).NetworkId;
                SpawnCharacter(entity, networkId, ecb, respawnZoneTransform.Position);


                ecb.RemoveComponent<WaitForRespawnTag>(entity);
            }
        }

        //if (resetStuffLookup.HasComponent(spawnerEntity))
        //{
        //    //TODO : Vider l'inventaire
        //    commandBuffer.RemoveComponent<ResetStuffTag>(playerEntity.Index, playerEntity);
        //}
    }

    public void SpawnCharacter(Entity client, int networkId, EntityCommandBuffer ecb, float3 position)
    {
        PrefabsData prefabManager = SystemAPI.GetSingleton<PrefabsData>();

        if (prefabManager.character == null)
        {
            return;
        }

        FixedString128Bytes worldName = ConnectionManager.Instance.Server.Name;

        Entity character = ecb.Instantiate(prefabManager.character);
        ecb.SetComponent(character, new LocalTransform() //Set position
        {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1.0f
        });
        ecb.SetComponent(character, new GhostOwner() //Set owner of player to connection
        {
            NetworkId = networkId
        });
        ecb.AppendToBuffer(client, new LinkedEntityGroup() //Link it to connection
        {
            Value = character
        });

        ecb.SetComponent(client, new ClientCharacterAttached { Value = character });
        ecb.SetComponent(character, new CharacterClientAttachedComponent { Value = client });

        ServerConsole.Log(ServerConsole.LogType.Info, $"Character spawned with NetworkId {networkId}, in the world {worldName}");
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

