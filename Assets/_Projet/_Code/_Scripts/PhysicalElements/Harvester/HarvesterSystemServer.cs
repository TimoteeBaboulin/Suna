using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static RoundSystemServer;

//TODO: Add animation handling
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
            Debug.LogError("[Server] Couldn't find round component for harvester systems");
            currentPhase = RoundPhase.ActionPhase;
        }

        foreach (var (newRoundTag, entity) in SystemAPI.Query<NewRoundTag>().WithEntityAccess())
        {
            ecb.RemoveComponent<NewRoundTag>(entity);
            newRound = true;
        }

        
        //Give the harvester to players if they don't have an owner already
        //TODO: Currently, the entities need to be spawned on the client for the RPCs to not get Entity.Null'd
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

            foreach (var (harvesterRW, harvesterEntity) in SystemAPI
                .Query<RefRW<HarvesterComponent>>()
                .WithEntityAccess())
            {
                if (corpoEntities.Length > 0)
                {
                    int random = UnityEngine.Random.Range(0, corpoEntities.Length);
                    Entity clientEntity = corpoEntities[random];
                    Entity characterEntity = SystemAPI.GetComponent<ClientCharacterAttached>(clientEntity).Value;
                    corpoEntities.RemoveAt(random);

                    harvesterRW.ValueRW.Owner = clientEntity;
                    SystemAPI.GetComponentRW<CharacterStuffList>(characterEntity).ValueRW.Value[(int)StuffType.Harvester] = harvesterEntity;
                    SystemAPI.GetComponentRW<StuffOwner>(harvesterEntity).ValueRW.Value = characterEntity;

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
            case RoundPhase.BuyPhase:
                {
                    //Other harvesters (owned or unowned)
                    foreach (var (harvesterRW, harvesterEntity) in
                        SystemAPI.Query<RefRW<HarvesterComponent>>()
                        .WithNone<HarvesterPlanting>()
                        .WithEntityAccess())
                    {
                        if (harvesterRW.ValueRO.Owner == Entity.Null)
                        {
                            if (currentTick.TicksSince(harvesterRW.ValueRO.DroppedTick) < 15)
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
                }
                break;
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
                }
                break;

            default:
                break;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
