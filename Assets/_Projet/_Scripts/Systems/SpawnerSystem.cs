using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

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

    int[] teamSpawnIndexes;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<ClientComponent, WaitForRespawnTag>();
        state.RequireForUpdate(state.GetEntityQuery(builder));

        respawnPtLookupInit = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        resetStuffLookupInit = state.GetComponentLookup<ResetStuffTag>(isReadOnly: true);

        teamSpawnIndexes = new int[2] { 0, 0 };
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        //Necessary data for efficient handling of respawns
        bool[] teamSpawnsValid = { false, false, false };
        Entity[] teamSpawnsEntities = new Entity[3];

        foreach (var (spawner, entity) in SystemAPI.Query<RefRO<TeamSpawnComponent>>().WithEntityAccess())
        {
            teamSpawnsValid[(int)spawner.ValueRO.team] = true;
            teamSpawnsEntities[(int)spawner.ValueRO.team] = entity;
        }

        foreach (var (playerComponent, clientEntity) in SystemAPI.Query<RefRW<ClientComponent>>().WithAll<WaitForRespawnTag>().WithEntityAccess())
        {

            //This is set up to allow easy team dispatching once it's implemented
            TeamSideType teamSideType = TeamSideType.Neutre;
            if (SystemAPI.HasComponent<CorpoTeamTag>(clientEntity))
            {
                teamSideType = TeamSideType.Corpo;
            }
            else if (SystemAPI.HasComponent<NatifTeamTag>(clientEntity))
            {
                teamSideType = TeamSideType.Natif;
            }

            //TODO: Let the client know its team so we can spawn in the right spawn
            teamSideType = (TeamSideType)UnityEngine.Random.Range(0, 2);

            if (!teamSpawnsValid[(int)teamSideType])
            {
                continue;
            }

            //Spawns are currently random but we might need to dispatch them in order with a counter getting incremented
            //Or a special procedure for new rounds
            Entity spawnerEntity = teamSpawnsEntities[(int)teamSideType];

            var buffer = SystemAPI.GetBuffer<SpawnPointBufferComponent>(teamSpawnsEntities[(int)teamSideType]);
            int index;
            if (teamSideType == TeamSideType.Neutre)
                index = UnityEngine.Random.Range(0, buffer.Length);
            else
            {
                index = teamSpawnIndexes[(int)teamSideType];
                teamSpawnIndexes[(int)teamSideType]++;
            }

            int networkId = state.EntityManager.GetComponentData<GhostOwner>(clientEntity).NetworkId;

            //SpawnCharacter(clientEntity, networkId, ecb, buffer[random]);
            //ecb.RemoveComponent<WaitForRespawnTag>(clientEntity);
            //ecb.RemoveComponent<WaitForRespawnTag>(clientEntity);

            //Spawn a new character if the client no longer has one, otherwise teleport it back to the start with full health
            Entity characterEntity = SystemAPI.GetComponent<ClientCharacterAttached>(clientEntity).Value;
            if (!state.EntityManager.Exists(characterEntity))
            {
                SpawnCharacter(clientEntity, networkId, ecb, buffer[index % buffer.Length]);
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

    public Entity SpawnCharacter(Entity client, int networkId, EntityCommandBuffer ecb, float3 position)
    {
        ClientPrefabData prefabManager = SystemAPI.GetSingleton<ClientPrefabData>();

        if (prefabManager.Character == null)
        {
            return Entity.Null;
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

        ServerConsole.Log(ServerConsole.LogType.Info, $"Character spawned with NetworkId {networkId}, in the world {worldName}");
        return character;
    }
}