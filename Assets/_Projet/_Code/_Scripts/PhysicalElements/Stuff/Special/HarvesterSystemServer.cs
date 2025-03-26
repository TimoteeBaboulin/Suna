using AK.Wwise;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static RoundSystemServer;

[UpdateAfter(typeof(RespawnSystem))]
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
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        //Prepare the current tick since it's used in multiple branches
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.InterpolationTick;

        bool newRound = false;
        RoundPhase currentPhase;

        if (SystemAPI.TryGetSingleton<RoundComponent>(out var roundComponent))
        {
            currentPhase = roundComponent.currentPhase;
        }
        else
        {
            Debug.LogError("Couldn't find round component for harvester systems");
            currentPhase = RoundPhase.ActionPhase;
        }

        foreach (var (newRoundTag, entity) in SystemAPI.Query<NewRoundTag>().WithEntityAccess())
        {
            ecb.RemoveComponent<NewRoundTag>(entity);
            newRound = true;
        }

        //Give the harvester to players if they don't have an owner already
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
                    Entity clientEntity = corpoEntities[random];
                    Entity characterEntity = SystemAPI.GetComponent<ClientCharacterAttached>(clientEntity).Value;
                    corpoEntities.RemoveAt(random);

                    harvesterRW.ValueRW.Owner = clientEntity;

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
                    harvesterRW.ValueRW.Owner = Entity.Null;
                    harvesterRW.ValueRW.DroppedTick = currentTick;
                    SystemAPI.GetComponentRW<LocalTransform>(harvesterEntity).ValueRW.Position = corpoSpawnPosition;
                }
            }
        }

        //Core handling of harvester events
        switch (currentPhase)
        {
            case RoundPhase.ActionPhase:
                {
                    //Other harvesters (owned or unowned)
                    foreach (var (harvesterRW, harvesterEntity) in 
                        SystemAPI.Query<RefRW<HarvesterComponent>>()
                        .WithNone<HarvesterPlanting>()
                        .WithEntityAccess())
                    {
                        if (harvesterRW.ValueRO.Owner == Entity.Null)
                        {
                            if (currentTick.TicksSince(harvesterRW.ValueRO.DroppedTick) < 1)
                                continue;

                            float3 harvesterPosition = state.EntityManager.GetComponentData<LocalTransform>(harvesterEntity).Position;

                            foreach ((LocalTransform playerTransform, RefRW<CharacterStuffList> stuffList, CharacterClientAttachedComponent clientAttached, Entity characterEntity)
                            in SystemAPI.Query<LocalTransform, RefRW<CharacterStuffList>, CharacterClientAttachedComponent>()
                            .WithAll<CharacterComponent>()
                            .WithEntityAccess())
                            {
                                if (math.distance(playerTransform.Position, harvesterPosition) <= 5)
                                {
                                    harvesterRW.ValueRW.Owner = clientAttached.ClientEntity;
                                    stuffList.ValueRW.Value[(int)StuffType.Harvester] = harvesterEntity;
                                    SystemAPI.GetComponentRW<StuffOwner>(harvesterEntity).ValueRW.Value = characterEntity;

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

                    //Harvesters currently being planted
                    foreach (var (harvesterRW, harvesterTransformRW, harvesterEntity) in
                        SystemAPI.Query<RefRW<HarvesterComponent>, RefRW<LocalTransform>>()
                        .WithAll<HarvesterPlanting>()
                        .WithEntityAccess())
                    {
                        if (currentTick.TicksSince(harvesterRW.ValueRO.PlantStartedTick) >= 60 * 4)
                        {
                            SystemAPI.SetComponentEnabled<HarvesterPlanting>(harvesterEntity, false);
                            ecb.SetComponentEnabled<HarvesterPlanted>(harvesterEntity, true);

                            Entity characterEntity = SystemAPI.GetComponentRO<ClientCharacterAttached>(harvesterRW.ValueRO.Owner).ValueRO.Value;
                            SystemAPI.GetComponentRW<CharacterStuffList>(characterEntity).ValueRW.Value[(int)StuffType.Harvester] = Entity.Null;
                            SystemAPI.GetComponentRW<StuffOwner>(harvesterEntity).ValueRW.Value = Entity.Null;
                            SystemAPI.GetComponentRW<CharacterStuffInHandType>(characterEntity).ValueRW.Value = StuffType.Melee;
                            SystemAPI.SetComponentEnabled<IsStuffInHand>(harvesterEntity, false);

                            harvesterTransformRW.ValueRW.Position = SystemAPI.GetComponentRO<LocalTransform>(characterEntity).ValueRO.Position;
                            harvesterRW.ValueRW.PlantedTick = currentTick;

                            RpcHarvesterPlanted rpc = new RpcHarvesterPlanted
                            {
                                harvester = harvesterEntity,
                                plantedTick = currentTick,
                                harvesterOwner = characterEntity
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

                            harvesterRW.ValueRW.Owner = Entity.Null;
                        }
                    }
                }
                break;

            case RoundPhase.PostPlantPhase:
            case RoundPhase.PostRoundPhase:
                {
                    //foreach (var (harvesterRW, harvesterEntity) in SystemAPI
                    //    .Query<RefRW<HarvesterComponent>>()
                    //    .WithNone<HarvesterPlanted>()
                    //    .WithEntityAccess())
                    //{

                    //}
                }
                break;
        }

        //RPC HANDLING____________________________________________________________________________________

        //TODO: Handle plant time and message relaying
        foreach ((RefRO<ReceiveRpcCommandRequest> request, HarvesterStartPlant rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, HarvesterStartPlant>().WithEntityAccess())
        {
            //TODO: Add zone and ownership checks to avoid planting someone else's harvester outside of a site
            //TODO: Add animation
            //Entity roundManagerEntity;

            ecb.SetComponentEnabled<HarvesterPlanting>(rpc.harvester, true);
            if (currentTick.TicksSince(rpc.tick) > 10)
            {
                SystemAPI.GetComponentRW<HarvesterComponent>(rpc.harvester).ValueRW.PlantStartedTick = currentTick;
            }
            else
            {
                SystemAPI.GetComponentRW<HarvesterComponent>(rpc.harvester).ValueRW.PlantStartedTick = rpc.tick;
            }

            ecb.DestroyEntity(entity);
        }
        foreach ((RefRO<ReceiveRpcCommandRequest> request, HarvesterStopPlant rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, HarvesterStopPlant>().WithEntityAccess())
        {
            ecb.SetComponentEnabled<HarvesterPlanting>(rpc.harvester, false);

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
