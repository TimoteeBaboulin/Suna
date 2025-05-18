using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;

public struct WaitForRespawnTag : IComponentData { }
public struct ResetStuffTag : IComponentData { }
public struct ShouldBeDropped : IComponentData 
{
    public float3 position;
    public float3 direction;
}
public struct ShouldBeDestroyed : IComponentData { }
public struct ShouldEmptyInventory : IComponentData { }

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct OnDieSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<HasNoHealthTag>();
        state.RequireForUpdate(state.GetEntityQuery(builder));

        state.RequireForUpdate<SpawnerSettingsTag>();
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
            autoRespawnIsEnable = SpawnerUtils.AutoRespawnIsEnable(ref state),
            resetStuffLookup = resetStuffLookupInit,
            HasNoHealthTagLookup = hasNoHealthTagLookup,
            CharacterEnabledLookup = SystemAPI.GetComponentLookup<CharacterIsEnable>(true),
            playerStuff = SystemAPI.GetBufferLookup<CharacterStuffList>(true),
            linkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true),
            ghostOwnerLookup = SystemAPI.GetComponentLookup<GhostOwner>(true),
            stuffDynamicDataLookup = SystemAPI.GetComponentLookup<StuffDynamicData>(true),
            shootStartPositionDeltaLookup = SystemAPI.GetComponentLookup<CharacterShootStartPositionDelta>(true),
            localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();
    }
}

[WithAll(typeof(CurrentHealthComponent), typeof(Simulate))]
public partial struct OnDieJob : IJobEntity
{
    public float dt;
    public NetworkTime networkTime;
    public EntityCommandBuffer.ParallelWriter commandBuffer;

    [ReadOnly] public bool autoRespawnIsEnable;
    [ReadOnly] public ComponentLookup<ResetStuffTag> resetStuffLookup;
    [ReadOnly] public ComponentLookup<HasNoHealthTag> HasNoHealthTagLookup;
    [ReadOnly] public ComponentLookup<CharacterIsEnable> CharacterEnabledLookup;
    [ReadOnly] public BufferLookup<CharacterStuffList> playerStuff;

    [ReadOnly] public BufferLookup<LinkedEntityGroup> linkedEntityGroupLookup;
    [ReadOnly] public ComponentLookup<GhostOwner> ghostOwnerLookup;
    [ReadOnly] public ComponentLookup<StuffDynamicData> stuffDynamicDataLookup;
    [ReadOnly] public ComponentLookup<CharacterShootStartPositionDelta> shootStartPositionDeltaLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;

    public void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, RefRO<CharacterClientAttachedComponent> CharacterPlayerAttached)
    {
        if (!resetStuffLookup.HasComponent(entity)
            && HasNoHealthTagLookup.HasComponent(entity)
            && CharacterEnabledLookup.IsComponentEnabled(entity))
        {
            commandBuffer.SetComponentEnabled<CharacterIsEnable>(sortKey, entity, false);
            commandBuffer.SetComponentEnabled<IsInstanciateDefaultStuff>(sortKey, entity, true); //Enable the default stuff instantiation at respawn

            //FIX (Aurelien) : Now that the player is dead, we drop some of his stuff, the rest gets destroyed

            shootStartPositionDeltaLookup.TryGetComponent(entity, out var shootStartPositionDelta);
            localTransformLookup.TryGetComponent(entity, out var localTransform);
            float3 playerPos = shootStartPositionDelta.PositionDelta + localTransform.Position;
            float3 playerDir = math.mul(localTransform.Rotation, math.forward());

            if (playerStuff.TryGetBuffer(entity, out var stuffList))
            {
                if (StuffUtils.GetStuffInSlot(stuffList, StuffSlot.MainWeapon) != Entity.Null)
                {
                    Entity stuff = StuffUtils.GetStuffInSlot(stuffList, StuffSlot.MainWeapon);

                    commandBuffer.AddComponent(sortKey, stuff, new ShouldBeDropped()
                    {
                        position = playerPos,
                        direction = playerDir
                    });

                    if (StuffUtils.GetStuffInSlot(stuffList, StuffSlot.SecondaryWeapon) != Entity.Null) //If the player has a secondary weapon, we destroy it
                        commandBuffer.AddComponent<ShouldBeDestroyed>(sortKey, StuffUtils.GetStuffInSlot(stuffList, StuffSlot.SecondaryWeapon));
                }
                else if(StuffUtils.GetStuffInSlot(stuffList, StuffSlot.SecondaryWeapon) != Entity.Null) //If the player has a secondary weapon but no main weapon, we drop the secondary weapon
                {
                    Entity stuff = StuffUtils.GetStuffInSlot(stuffList, StuffSlot.SecondaryWeapon);

                    commandBuffer.AddComponent(sortKey, stuff, new ShouldBeDropped()
                    {
                        position = playerPos,
                        direction = playerDir
                    });
                }

                commandBuffer.AddComponent<ShouldBeDestroyed>(sortKey, StuffUtils.GetStuffInSlot(stuffList, StuffSlot.Melee)); //We destroy the knife (so it doesn't get duplicated on respawn)

                if (StuffUtils.GetStuffInSlot(stuffList, StuffSlot.Harvester) != Entity.Null) //Drops the Harvester if player has it
                {
                    Entity stuff = StuffUtils.GetStuffInSlot(stuffList, StuffSlot.Harvester);

                    commandBuffer.AddComponent(sortKey, stuff, new ShouldBeDropped()
                    {
                        position = playerPos,
                        direction = playerDir
                    });
                }

                bool stuffDropped = false;

                for (int i = (int)StuffSlot.HEGrenade; i < (int)StuffSlot.nbSlots; i++)
                {
                    if (StuffUtils.GetStuffInSlot(stuffList, (StuffSlot)i) != Entity.Null)
                    {
                        if (!stuffDropped)
                        {
                            commandBuffer.AddComponent(sortKey, StuffUtils.GetStuffInSlot(stuffList, (StuffSlot)i), new ShouldBeDropped()
                            {
                                position = playerPos,
                                direction = playerDir
                            });
                            stuffDropped = true;
                        }
                        else
                        {
                            //commandBuffer.DestroyEntity(sortKey, stuffList.List[i]);
                            commandBuffer.AddComponent<ShouldBeDestroyed>(sortKey, StuffUtils.GetStuffInSlot(stuffList, (StuffSlot)i));
                        }
                    }
                }

                //for (int i = 0; i < (int)StuffSlot.nbSlots; i++)
                //{
                //    if (StuffUtils.GetStuffInSlot(stuffList, (StuffSlot)i) != Entity.Null)
                //    {
                //        UnequipStuff(StuffUtils.GetStuffInSlot(stuffList, (StuffSlot)i), entity);
                //    }
                //}

                //commandBuffer.AddComponent<ShouldEmptyInventory>(sortKey, entity);
            }

            if (autoRespawnIsEnable)
            {
                commandBuffer.AddComponent<WaitForRespawnTag>(sortKey, CharacterPlayerAttached.ValueRO.ClientEntity);
            }
        }
    }

    private void UnequipStuff(Entity stuff, Entity player)
    {
        if(stuffDynamicDataLookup.TryGetComponent(stuff, out var stuffDynamicData))
        {
            stuffDynamicData.owner = Entity.Null;
        }

        if(ghostOwnerLookup.TryGetComponent(stuff, out var ghostOwner))
        {
            ghostOwner.NetworkId = -1;
        }

        if (linkedEntityGroupLookup.TryGetBuffer(stuff, out var linkedEntityGroup))
        {
            for (int i = 0; i < linkedEntityGroup.Length; i++)
            {
                if (linkedEntityGroup[i].Value == player)
                {
                    linkedEntityGroup.RemoveAt(i);
                    break;
                }
            }
        }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateAfter(typeof(ServerSystem))]
public partial struct RespawnSystem : ISystem
{
    ComponentLookup<LocalTransform> respawnPtLookupInit;
    ComponentLookup<ResetStuffTag> resetStuffLookupInit;

    NativeList<int> teamSpawnIndexes;

    //[BurstCompile]
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

        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();

        foreach (var (spawner, entity) in SystemAPI.Query<RefRO<TeamSpawnComponent>>().WithEntityAccess())
        {
            teamSpawnsValid[(int)spawner.ValueRO.team] = true;
            teamSpawnsEntities[(int)spawner.ValueRO.team] = entity;
        }

        foreach (var (playerComponent, clientEntity) in SystemAPI.Query<RefRW<ClientComponent>>().WithAll<WaitForRespawnTag>().WithEntityAccess())
        {
            
            int networkId = SystemAPI.GetComponent<GhostOwner>(clientEntity).NetworkId;
            TeamSideType teamSideType = playerComponent.ValueRW.team;
            if (teamSideType == TeamSideType.Neutre)
            {
                continue;
            }

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
                SpawnCharacter(clientEntity, networkId, ecb, buffer[index % buffer.Length], teamSideType);
                ecb.RemoveComponent<WaitForRespawnTag>(clientEntity);
            }
            else if (state.EntityManager.HasComponent<LocalTransform>(characterEntity))
            {
                RefRW<LocalTransform> transform = SystemAPI.GetComponentRW<LocalTransform>(characterEntity);
                RefRW<CurrentHealthComponent> currentHealth = SystemAPI.GetComponentRW<CurrentHealthComponent>(characterEntity);
                transform.ValueRW.Position = buffer[index % buffer.Length];
                currentHealth.ValueRW.Value = 100;

                ////FIX (Aurelien) : When the player dies, he respawns with nothing (if he dropped his stuff)

                //if(SystemAPI.GetBuffer<CharacterStuffList>(characterEntity).ElementAt((int)StuffSlot.SecondaryWeapon).entity == Entity.Null) //Avoid duplicating gun if for some reason the player already has one
                //{
                //    SystemAPI.TryGetSingletonBuffer<InstantiateStuffQueue>(out var queue);
                //    StuffUtils.InstantiateNextFrame(queue, "LP-17", characterEntity); //The player gets to respawn with a handgun
                //}

                ecb.SetComponentEnabled<CharacterIsEnable>(characterEntity, true);
                ecb.RemoveComponent<HasNoHealthTag>(characterEntity);
                ecb.RemoveComponent<WaitForRespawnTag>(clientEntity);

                var stuffBuffer = SystemAPI.GetBuffer<CharacterStuffList>(characterEntity);
                for (int i = 0; i < (int)StuffSlot.Melee; i++) //Only reset the main weapon and the secondary weapon
                {
                    if (stuffBuffer[i].entity != Entity.Null)
                    {
                        var stuffDatabase = SystemAPI.GetComponentRO<RangedWeaponDatabaseAccess>(stuffBuffer[i].entity);
                        var stuffDynamicData = SystemAPI.GetComponentRW<RangedWeaponDynamicData>(stuffBuffer[i].entity);
                        ref RangedWeaponCommonData commonData = ref stuffDatabase.ValueRO.GetData(ref database);

                        stuffDynamicData.ValueRW.currentAmmo = commonData.magazineCapacity + 1;
                        stuffDynamicData.ValueRW.remainingAmmo = (commonData.nbMagazine - 1) * commonData.magazineCapacity;
                        // Make sure to set the ammo to the max capacity
                    }
                }
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
        ecb.SetComponent(character, new GhostOwner
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
            case TeamSideType.Corpo:
                ecb.AddComponent<CorpoTeamTag>(character);
                break;
            case TeamSideType.Natif:
                ecb.AddComponent<NatifTeamTag>(character);
                break;
            default:
                break;
        }

        ServerConsole.Log(ServerConsole.LogType.Info, $"Character spawned with NetworkId {networkId}, in the world {worldName}");
    }
}
