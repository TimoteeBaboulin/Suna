using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

//TODO: Add animation handling
//Maybe use a RPC to request more data
[UpdateAfter(typeof(RespawnSystem))]
[UpdateBefore(typeof(InstanciateEntityStuffSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct HarvesterSystemServer : ISystem
{
    bool harvesterIsInstantiated;
    int frameCounter;

    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        harvesterIsInstantiated = false;
        frameCounter = 60;

        state.RequireForUpdate<UnequipStuffQueue>();
        state.RequireForUpdate<EquipStuffQueue>();
        state.RequireForUpdate<InstantiateStuffQueue>();
    }

    float3 GetRandomHarvesterSpawn(ref SystemState state)
    {
        foreach (var (spawn, spawnEntity) in SystemAPI.Query<TeamSpawnComponent>().WithEntityAccess())
        {
            if (spawn.team == TeamSideType.Corpo)
            {
                var buffer = SystemAPI.GetBuffer<SpawnPointBufferComponent>(spawnEntity);
                int random = UnityEngine.Random.Range(0, buffer.Length);
                return buffer[random];
            }
        }

        Debug.LogWarning("[Server] Harvester couldn't find corporation spawn, default spawn set to [0,0,0]");
        return float3.zero;
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var instantiateStuffQueue = SystemAPI.GetSingletonBuffer<InstantiateStuffQueue>();
        var equipStuffQueue = SystemAPI.GetSingletonBuffer<EquipStuffQueue>();
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        //if (!SystemAPI.TryGetSingletonBuffer<EquipStuffQueue>(out var equipStuffQueu) || !SystemAPI.TryGetSingletonBuffer<UnequipStuffQueue>(out var unequipStuffQueu))
        //{
        //    // Debug.Log("Can't handle harvester spawn since equip and unequip queues are not loaded yet");
        //    return;
        //}


        if (!harvesterIsInstantiated)
        {
            if (SystemAPI.TryGetSingletonBuffer<InstantiateStuffQueue>(out var queue))
            {
                StuffUtils.InstantiateNextFrame(queue, "Harvester", new float3(40f, 0f, 2f));

                harvesterIsInstantiated = true;
            }

            return;
        }

        //Prepare the current tick since it's used in multiple branches
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime))
            return;

        NetworkTick currentTick = networkTime.InterpolationTick;
        if (!currentTick.IsValid)
            return;

        RoundPhase currentPhase;

        if (SystemAPI.TryGetSingleton<RoundComponent>(out var roundComponent))
        {
            currentPhase = roundComponent.currentPhase;
        }
        else
        {
            Debug.LogError("[Server] Couldn't find round component for harvester systems");
            currentPhase = RoundPhase.ActionPhase;
        }


        //Give the harvester to players if they don't have an owner already
        //TODO: Currently, the entities need to be spawned on the client for the RPCs to not get Entity.Null'd
        NativeList<Entity> corpoEntities = new NativeList<Entity>(Allocator.Temp);
        float3 corpoSpawnPosition = GetRandomHarvesterSpawn(ref state);

        foreach (var (playerComponent, clientEntity) in SystemAPI.Query<RefRW<ClientComponent>>().WithEntityAccess())
        {
            if (playerComponent.ValueRO.team == TeamSideType.Corpo)
                corpoEntities.Add(clientEntity);
        }

        foreach ((RefRW<HarvesterComponent> harvesterRW, RefRO<StuffDynamicData> ownerRO, Entity harvesterEntity) in SystemAPI
            .Query<RefRW<HarvesterComponent>, RefRO<StuffDynamicData>>()
            .WithAll<HarvesterRespawn>()
            .WithEntityAccess())
        {
            Debug.Log("Respawning harvester");

            if (corpoEntities.Length > 0)
            {
                //Equip the harvester to a random player (without forgetting to unequip it if it's already equipped to someone else)
                int random = UnityEngine.Random.Range(0, corpoEntities.Length);
                Entity clientEntity = corpoEntities[random];
                Entity characterEntity = SystemAPI.GetComponent<ClientCharacterAttached>(clientEntity).Value;
                corpoEntities.RemoveAt(random);

                StuffUtils.EquipNextFrame(equipStuffQueue, characterEntity, harvesterEntity, true);

                //StuffUtils.InstantiateNextFrame(instantiateStuffQueue, "Harvester", characterEntity);
                //SpawnHarvesterOnCharacter(ref state, characterEntity, harvesterEntity, unequipStuffQueu, equipStuffQueu);
            }
            //else
            //{
            //    StuffUtils.DropNextFrame(ref state, characterEntity, harvesterEntity, true);
            //    StuffUtils.InstantiateNextFrame(instantiateStuffQueue, "Harvester", corpoSpawnPosition);
            //    //SpawnHarvesterInMap(ref state, harvesterEntity, corpoSpawnPosition, currentTick, unequipStuffQueu);
            //}

            ecb.RemoveComponent<HarvesterRespawn>(harvesterEntity);
        }


        //Core handling of harvester events
        switch (currentPhase)
        {
            case RoundPhase.BuyPhase:
            case RoundPhase.ActionPhase:
                {
                    //Other harvesters (owned or unowned)
                    foreach (var (harvesterRW, ownerRW, harvesterEntity) in
                        SystemAPI.Query<RefRW<HarvesterComponent>, RefRW<StuffDynamicData>>()
                        .WithNone<HarvesterPlanting, HarvesterRespawn>()
                        .WithEntityAccess())
                    {
                        if (!harvesterRW.ValueRO.IsActive)
                            continue;

                        if (ownerRW.ValueRO.owner == Entity.Null)
                        {
                            if (currentTick.TicksSince(harvesterRW.ValueRO.DroppedTick) < 15)
                                continue;

                            float3 harvesterPosition = state.EntityManager.GetComponentData<LocalTransform>(harvesterEntity).Position;

                            foreach ((LocalTransform playerTransform, DynamicBuffer<CharacterStuffList> stuffList, CharacterClientAttachedComponent clientAttached, Entity characterEntity)
                            in SystemAPI.Query<LocalTransform, DynamicBuffer<CharacterStuffList>, CharacterClientAttachedComponent>()
                            .WithAll<CharacterComponent>()
                            .WithEntityAccess())
                            {
                                ClientComponent client = SystemAPI.GetComponent<ClientComponent>(clientAttached.ClientEntity);

                                if (client.team == TeamSideType.Corpo && math.distance(playerTransform.Position, harvesterPosition) <= harvesterRW.ValueRO.pickupDistance)
                                {
                                    //EquipHarvester(ref state, characterEntity, harvesterEntity, unequipStuffQueu, equipStuffQueu);
                                    StuffUtils.EquipNextFrame(equipStuffQueue, characterEntity, harvesterEntity, false);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                break;

            default:
                break;
        }


        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcRequestHarvesterOwners rpc, Entity entity)
    in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcRequestHarvesterOwners>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);

            foreach ((RefRO<HarvesterComponent> harvester, RefRO<StuffDynamicData> ownerRO, Entity harvesterEntity) in SystemAPI
                .Query<RefRO<HarvesterComponent>, RefRO<StuffDynamicData>>()
                .WithEntityAccess())
            {
                if (ownerRO.ValueRO.owner == Entity.Null)
                    continue;

                RpcHarvesterOwnerChange response = new RpcHarvesterOwnerChange
                {
                    harvester = harvesterEntity,
                    character = ownerRO.ValueRO.owner
                };

                Entity rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, response);
                ecb.AddComponent(rpcEntity, new SendRpcCommandRequest
                {
                    TargetConnection = request.ValueRO.SourceConnection
                });
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    //void EquipHarvester(ref SystemState state, Entity characterEntity, Entity harvesterEntity, DynamicBuffer<UnequipStuffQueue> unequipBuffer, DynamicBuffer<EquipStuffQueue> equipBuffer)
    //{
    //    RefRO<StuffDynamicData> harvesterDynamicDataRO = SystemAPI.GetComponentRO<StuffDynamicData>(harvesterEntity);

    //    if (harvesterDynamicDataRO.ValueRO.owner != Entity.Null)
    //    {
    //        StuffUtils.UnequipNextFrame(unequipBuffer, harvesterDynamicDataRO.ValueRO.owner, harvesterEntity);
    //    }

    //    StuffUtils.EquipNextFrame(equipBuffer, characterEntity, harvesterEntity, true);

    //    RpcHarvesterOwnerChange rpc = new RpcHarvesterOwnerChange
    //    {
    //        harvester = harvesterEntity,
    //        character = characterEntity
    //    };
    //    EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<ClientComponent>().Build(ref state);

    //    foreach (var client in query.ToEntityArray(Allocator.Temp))
    //    {
    //        RpcUtils.SendServerToClientRpc(ref rpc, client);
    //    }
    //}

    //void SpawnHarvesterOnCharacter(ref SystemState state, Entity characterEntity, Entity harvesterEntity, DynamicBuffer<UnequipStuffQueue> unequipBuffer, DynamicBuffer<EquipStuffQueue> equipBuffer)
    //{
    //    EquipHarvester(ref state, characterEntity, harvesterEntity, unequipBuffer, equipBuffer);

    //    RpcHarvesterOwnerChange rpc = new RpcHarvesterOwnerChange
    //    {
    //        harvester = harvesterEntity,
    //        character = characterEntity
    //    };
    //    EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<ClientComponent>().Build(ref state);

    //    foreach (var client in query.ToEntityArray(Allocator.Temp))
    //    {
    //        RpcUtils.SendServerToClientRpc(ref rpc, client);
    //    }
    //}

    //void SpawnHarvesterInMap(ref SystemState state, Entity harvesterEntity, float3 position, NetworkTick currentTick, DynamicBuffer<UnequipStuffQueue> unequipBuffer)
    //{
    //    RefRO<StuffDynamicData> harvesterDynamicDataRO = SystemAPI.GetComponentRO<StuffDynamicData>(harvesterEntity);
    //    RefRW<HarvesterComponent> harvesterRW = SystemAPI.GetComponentRW<HarvesterComponent>(harvesterEntity);

    //    if (harvesterDynamicDataRO.ValueRO.owner != Entity.Null)
    //    {
    //        unequipBuffer.Add(new UnequipStuffQueue
    //        {
    //            Owner = harvesterDynamicDataRO.ValueRO.owner,
    //            Stuff = harvesterEntity,
    //        });
    //    }

    //    harvesterRW.ValueRW.DroppedTick = currentTick;
    //    harvesterRW.ValueRW.IsActive = true;

    //    RpcHarvesterDropped rpc = new RpcHarvesterDropped
    //    {
    //        harvester = harvesterEntity,
    //        position = position
    //    };
    //    EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<ClientComponent>().Build(ref state);

    //    foreach (var client in query.ToEntityArray(Allocator.Temp))
    //    {
    //        RpcUtils.SendServerToClientRpc(ref rpc, client);
    //    }

    //    SystemAPI.GetComponentRW<LocalTransform>(harvesterEntity).ValueRW.Position = position;
    //}

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
