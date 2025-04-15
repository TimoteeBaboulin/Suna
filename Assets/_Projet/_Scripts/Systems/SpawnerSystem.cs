using GameNetwork.Utils;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Services.Multiplayer;
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
            commandBuffer.SetComponentEnabled<CharacterIsEnable>(sortKey, entity, false);
            commandBuffer.AddComponent<WaitForRespawnTag>(sortKey, CharacterPlayerAttached.ValueRO.ClientEntity);
            //commandBuffer.RemoveComponent<HasNoHealthTag>(sortKey, entity);
            //commandBuffer.DestroyEntity(sortKey, entity);

            //commandBuffer.AddComponent<ResetStuffTag>(sortKey, entity);
        }
    }
}



[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct RespawnSystem : ISystem
{
    ComponentLookup<LocalTransform> respawnPtLookupInit;
    ComponentLookup<ResetStuffTag> resetStuffLookupInit;

    NativeList<int> teamSpawnIndexes;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<ClientComponent, WaitForRespawnTag>();
        state.RequireForUpdate(state.GetEntityQuery(builder));

        respawnPtLookupInit = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        resetStuffLookupInit = state.GetComponentLookup<ResetStuffTag>(isReadOnly: true);

        teamSpawnIndexes = new NativeList<int>(Allocator.Persistent);
        teamSpawnIndexes.Add(0);
        teamSpawnIndexes.Add(0);
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        bool[] teamSpawnsValid = { false, false, false };
        Entity[] teamSpawnsEntities = new Entity[3];

        foreach (var (spawner, entity) in SystemAPI.Query<RefRO<TeamSpawnComponent>>().WithEntityAccess())
        {
            teamSpawnsValid[(int)spawner.ValueRO.team] = true;
            teamSpawnsEntities[(int)spawner.ValueRO.team] = entity;
        }

        foreach (var (playerComponent, clientEntity) in SystemAPI.Query<RefRW<ClientComponent>>().WithAll<WaitForRespawnTag>().WithEntityAccess())
        {
            int networkId = SystemAPI.GetComponent<GhostOwner>(clientEntity).NetworkId;
            TeamSideType teamSideType = playerComponent.ValueRW.team;
            Debug.Log($"[AliveCheck] Final teamSideType from ClientComponent: {teamSideType} for networkId {networkId}");

            if (!teamSpawnsValid[(int)teamSideType])
            {
                continue;
            }

            Entity spawnerEntity = teamSpawnsEntities[(int)teamSideType];
            var buffer = SystemAPI.GetBuffer<SpawnPointBufferComponent>(spawnerEntity);

            int index;
            if (teamSideType == TeamSideType.Neutre)
            {
                index = UnityEngine.Random.Range(0, buffer.Length);
            }
            else
            {
                index = teamSpawnIndexes[(int)teamSideType];
                teamSpawnIndexes[(int)teamSideType]++;
            }

            Entity characterEntity = SystemAPI.GetComponent<ClientCharacterAttached>(clientEntity).Value;
            if (!state.EntityManager.Exists(characterEntity))
            {
                playerComponent.ValueRW.networkID = networkId;

                SpawnCharacter(clientEntity, networkId, ecb, buffer[index % buffer.Length], teamSideType);
                ecb.RemoveComponent<WaitForRespawnTag>(clientEntity);
            }
            else if (state.EntityManager.HasComponent<LocalTransform>(characterEntity))
            {
                RefRW<LocalTransform> transform = SystemAPI.GetComponentRW<LocalTransform>(characterEntity);
                RefRW<CurrentHealthComponent> currentHealth = SystemAPI.GetComponentRW<CurrentHealthComponent>(characterEntity);
                transform.ValueRW.Position = buffer[index % buffer.Length];
                currentHealth.ValueRW.Value = 100;

                ecb.SetComponentEnabled<CharacterIsEnable>(characterEntity, true);
                ecb.RemoveComponent<HasNoHealthTag>(characterEntity);
                ecb.RemoveComponent<WaitForRespawnTag>(clientEntity);
            }
        }
    }

    public void SpawnCharacter(Entity client, int networkId, EntityCommandBuffer ecb, float3 position, TeamSideType team)
    {
        ClientPrefabData prefabManager = SystemAPI.GetSingleton<ClientPrefabData>();

        if (prefabManager.Character == null)
        {
            return;
        }

        FixedString128Bytes worldName = ClientServerBootstrap.ServerWorld.Name;

        Entity character = ecb.Instantiate(prefabManager.Character);
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
        ecb.SetComponent(character, new CharacterClientAttachedComponent { ClientEntity = client });
        switch (team)
        {
            case TeamSideType.Corpo: ecb.AddComponent<CorpoTeamTag>(character); break;
            case TeamSideType.Natif: ecb.AddComponent<NatifTeamTag>(character); break;
            default: break;
        }

        ServerConsole.Log(ServerConsole.LogType.Info, $"Character spawned with NetworkId {networkId}, in the world {worldName}");
    }
}