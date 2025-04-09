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
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        if (!SystemAPI.TryGetSingletonBuffer<EquipStuffQueu>(out var equipStuffQueu) || !SystemAPI.TryGetSingletonBuffer<UnequipStuffQueu>(out var unequipStuffQueu))
        {
            // Debug.Log("Can't handle harvester spawn since equip and unequip queues are not loaded yet");
            return;
        }


        if (!harvesterIsInstantiated)
        {
            if (SystemAPI.TryGetSingletonBuffer<GameResourcesInstantiateStuffQueu>(out var queue))
            {
                queue.Add(new GameResourcesInstantiateStuffQueu
                {
                    StuffName = "Harvester",
                    Owner = Entity.Null
                });

                harvesterIsInstantiated = true;
                return;
            }
            else
            {
                return;
            }
        }


            //Prepare the current tick since it's used in multiple branches

            if (SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime))
            {
                NetworkTick currentTick = networkTime.InterpolationTick;
                if (!currentTick.IsValid)
                    return;

                //var networkTime = SystemAPI.GetSingleton<NetworkTime>();

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

                foreach ((RefRW<HarvesterComponent> harvesterRW, RefRW<StuffOwner> ownerRW, Entity harvesterEntity) in SystemAPI
                    .Query<RefRW<HarvesterComponent>, RefRW<StuffOwner>>()
                    .WithAll<HarvesterRespawn>()
                    .WithEntityAccess())
                {
                    if (corpoEntities.Length > 0)
                    {
                        Debug.Log("Spawning on character");
                        int random = UnityEngine.Random.Range(0, corpoEntities.Length);
                        Entity clientEntity = corpoEntities[random];
                        Entity characterEntity = SystemAPI.GetComponent<ClientCharacterAttached>(clientEntity).Value;
                        corpoEntities.RemoveAt(random);

                        //ownerRW.ValueRW.Value = clientEntity;
                        //SystemAPI.GetComponentRW<CharacterStuffList>(characterEntity).ValueRW.Value[(int)StuffInventoryLocation.Harvester] = harvesterEntity;
                        //SystemAPI.GetComponentRW<StuffOwner>(harvesterEntity).ValueRW.Value = characterEntity;

                        equipStuffQueu.Add(new EquipStuffQueu
                        {
                            Stuff = harvesterEntity,
                            Owner = characterEntity
                        });

                        RpcHarvesterOwnerChange rpc = new RpcHarvesterOwnerChange
                        {
                            harvester = harvesterEntity,
                            newOwner = clientEntity,
                            character = characterEntity
                        };
                        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<InitializedClient>().Build(ref state);


                        foreach (var client in query.ToEntityArray(Allocator.Temp))
                        {
                            Entity rpcEntity = ecb.CreateEntity();
                            ecb.AddComponent(rpcEntity, rpc);
                            ecb.AddComponent(rpcEntity, new SendRpcCommandRequest
                            {
                                TargetConnection = client
                            });
                        }
                        break;
                    }
                    else
                    {
                        Debug.Log("Spawning at corpo");
                        ownerRW.ValueRW.Value = Entity.Null;
                        harvesterRW.ValueRW.DroppedTick = currentTick;
                        harvesterRW.ValueRW.IsActive = true;
                        SystemAPI.GetComponentRW<LocalTransform>(harvesterEntity).ValueRW.Position = corpoSpawnPosition;
                    }
                    ecb.RemoveComponent<HarvesterRespawn>(harvesterEntity);
                }


                //Core handling of harvester events
                switch (currentPhase)
                {
                    case RoundPhase.BuyPhase:
                        {
                            //Other harvesters (owned or unowned)
                            foreach (var (harvesterRW, ownerRW, harvesterEntity) in
                                SystemAPI.Query<RefRW<HarvesterComponent>, RefRW<StuffOwner>>()
                                .WithNone<HarvesterPlanting, HarvesterRespawn>()
                                .WithEntityAccess())
                            {
                                if (!harvesterRW.ValueRO.IsActive)
                                    continue;

                                if (ownerRW.ValueRO.Value == Entity.Null)
                                {
                                    if (currentTick.TicksSince(harvesterRW.ValueRO.DroppedTick) < 15)
                                        continue;

                                    float3 harvesterPosition = state.EntityManager.GetComponentData<LocalTransform>(harvesterEntity).Position;

                                    foreach ((LocalTransform playerTransform, RefRW<CharacterStuffList> stuffList, CharacterClientAttachedComponent clientAttached, Entity characterEntity)
                                    in SystemAPI.Query<LocalTransform, RefRW<CharacterStuffList>, CharacterClientAttachedComponent>()
                                    .WithAll<CharacterComponent, CorpoTeamTag>()
                                    .WithEntityAccess())
                                    {
                                        if (math.distance(playerTransform.Position, harvesterPosition) <= harvesterRW.ValueRO.pickupDistance)
                                        {
                                            //ownerRW.ValueRW.Value = clientAttached.ClientEntity;
                                            //stuffList.ValueRW.Value[(int)StuffType.Harvester] = harvesterEntity;
                                            //SystemAPI.GetComponentRW<StuffOwner>(harvesterEntity).ValueRW.Value = characterEntity;
                                            equipStuffQueu.Add(new EquipStuffQueu
                                            {
                                                Stuff = harvesterEntity,
                                                Owner = characterEntity
                                            });
                                            RpcHarvesterOwnerChange rpc = new RpcHarvesterOwnerChange
                                            {
                                                harvester = harvesterEntity,
                                                newOwner = clientAttached.ClientEntity,
                                                character = characterEntity
                                            };
                                            EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<InitializedClient>().Build(ref state);

                                            foreach (var client in query.ToEntityArray(Allocator.Temp))
                                            {
                                                Entity rpcEntity = ecb.CreateEntity();
                                                ecb.AddComponent(rpcEntity, rpc);
                                                ecb.AddComponent(rpcEntity, new SendRpcCommandRequest
                                                {
                                                    TargetConnection = client
                                                });
                                            }
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
                    case RoundPhase.ActionPhase:
                        {
                            //Other harvesters (owned or unowned)
                            foreach (var (harvesterRW, ownerRW, harvesterEntity) in
                                SystemAPI.Query<RefRW<HarvesterComponent>, RefRW<StuffOwner>>()
                                .WithNone<HarvesterPlanting, HarvesterRespawn>()
                                .WithEntityAccess())
                            {
                                if (!harvesterRW.ValueRO.IsActive)
                                    continue;

                                if (ownerRW.ValueRO.Value == Entity.Null)
                                {
                                    if (currentTick.TicksSince(harvesterRW.ValueRO.DroppedTick) < 1)
                                        continue;

                                    float3 harvesterPosition = state.EntityManager.GetComponentData<LocalTransform>(harvesterEntity).Position;

                                    foreach ((LocalTransform playerTransform, RefRW<CharacterStuffList> stuffList, CharacterClientAttachedComponent clientAttached, Entity characterEntity)
                                    in SystemAPI.Query<LocalTransform, RefRW<CharacterStuffList>, CharacterClientAttachedComponent>()
                                    .WithAll<CharacterComponent, CorpoTeamTag>()
                                    .WithEntityAccess())
                                    {
                                        if (math.distance(playerTransform.Position, harvesterPosition) <= 5)
                                        {
                                            //ownerRW.ValueRW.Value = clientAttached.ClientEntity;
                                            //stuffList.ValueRW.Value[(int)StuffType.Harvester] = harvesterEntity;
                                            //SystemAPI.GetComponentRW<StuffOwner>(harvesterEntity).ValueRW.Value = characterEntity;
                                            equipStuffQueu.Add(new EquipStuffQueu
                                            {
                                                Stuff = harvesterEntity,
                                                Owner = characterEntity
                                            });
                                            RpcHarvesterOwnerChange rpc = new RpcHarvesterOwnerChange
                                            {
                                                harvester = harvesterEntity,
                                                newOwner = clientAttached.ClientEntity,
                                                character = characterEntity
                                            };
                                            EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<InitializedClient>().Build(ref state);

                                            foreach (var client in query.ToEntityArray(Allocator.Temp))
                                            {
                                                Entity rpcEntity = ecb.CreateEntity();
                                                ecb.AddComponent(rpcEntity, rpc);
                                                ecb.AddComponent(rpcEntity, new SendRpcCommandRequest
                                                {
                                                    TargetConnection = client
                                                });
                                            }
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
            }

            foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcRequestHarvesterOwners rpc, Entity entity)
        in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcRequestHarvesterOwners>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);

                foreach ((RefRO<HarvesterComponent> harvester, RefRO<StuffOwner> ownerRO, Entity harvesterEntity) in SystemAPI
                    .Query<RefRO<HarvesterComponent>, RefRO<StuffOwner>>()
                    .WithEntityAccess())
                {
                    if (ownerRO.ValueRO.Value == Entity.Null)
                        continue;

                    RpcHarvesterOwnerChange response = new RpcHarvesterOwnerChange
                    {
                        harvester = harvesterEntity,
                        newOwner = SystemAPI.GetComponentRO<CharacterClientAttachedComponent>(ownerRO.ValueRO.Value).ValueRO.ClientEntity,
                        character = ownerRO.ValueRO.Value
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

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
