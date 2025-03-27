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
    Entity defuserEntity;
    Entity defusingHarvesterEntity;
    NetworkTick defuseStartTick;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        defuserEntity = Entity.Null;
        defusingHarvesterEntity = Entity.Null;

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
                        //TODO: Prevent planting while moving or prevent moving while planting
                        if (currentTick.TicksSince(harvesterRW.ValueRO.PlantStartedTick) >= 60 * 4)
                        {
                            //TODO: Make the harvester owner automatically switch to primary, secondary or melee based on availability
                            SystemAPI.SetComponentEnabled<HarvesterPlanting>(harvesterEntity, false);
                            ecb.SetComponentEnabled<HarvesterPlanted>(harvesterEntity, true);

                            Entity characterEntity = SystemAPI.GetComponentRO<ClientCharacterAttached>(harvesterRW.ValueRO.Owner).ValueRO.Value;
                            SystemAPI.GetComponentRW<CharacterStuffList>(characterEntity).ValueRW.Value[(int)StuffType.Harvester] = Entity.Null;
                            SystemAPI.GetComponentRW<StuffOwner>(harvesterEntity).ValueRW.Value = Entity.Null;
                            SystemAPI.GetComponentRW<CharacterStuffInHandType>(characterEntity).ValueRW.Value = StuffType.Melee;
                            SystemAPI.SetComponentEnabled<IsStuffInHand>(harvesterEntity, false);

                            //TODO: Spawn the harvester on the ground instead, and sync position on every client
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
                {
                    if (defusingHarvesterEntity == Entity.Null)
                        break;

                    if (currentTick.TicksSince(defuseStartTick) < 4 * 60)
                    {
                        float3 harvesterPos = SystemAPI.GetComponentRO<LocalTransform>(defusingHarvesterEntity).ValueRO.Position;
                        float3 defuserPos = SystemAPI.GetComponentRO<LocalTransform>(defuserEntity).ValueRO.Position;

                        //TODO: Add other reasons to stop defusing (death of defuser, moving, jumping, trying to shoot...)
                        if (math.distance(harvesterPos, defuserPos) > 10)
                        {
                            defusingHarvesterEntity = Entity.Null;
                            defuserEntity = Entity.Null;

                            Debug.Log("Stopped defusing because of distance");
                        }

                        break;
                    }

                    //Defused
                    SystemAPI.SetComponentEnabled<HarvesterPlanted>(defusingHarvesterEntity, false);

                    defusingHarvesterEntity = Entity.Null;
                    defuserEntity = Entity.Null;
                    Debug.Log("Harvester was defused");
                }
                break;
            case RoundPhase.PostRoundPhase:
                {
                }
                break;
        }

        //RPC HANDLING____________________________________________________________________________________
        //Plant Start
        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcHarvesterPlantStart rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterPlantStart>().WithEntityAccess())
        {
            //TODO: Add zone and ownership checks to avoid planting someone else's harvester outside of a site
            //Entity roundManagerEntity;
            ecb.DestroyEntity(entity);
            if (currentPhase is not RoundPhase.ActionPhase or RoundPhase.PostRoundPhase)
            {
                continue;
            }

            ecb.SetComponentEnabled<HarvesterPlanting>(rpc.harvester, true);

            if (currentTick.TicksSince(rpc.tick) > 10)
            {
                SystemAPI.GetComponentRW<HarvesterComponent>(rpc.harvester).ValueRW.PlantStartedTick = currentTick;
            }
            else
            {
                SystemAPI.GetComponentRW<HarvesterComponent>(rpc.harvester).ValueRW.PlantStartedTick = rpc.tick;
            }
        }
        //Plant Stop
        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcHarvesterPlantStop rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterPlantStop>().WithEntityAccess())
        {
            ecb.SetComponentEnabled<HarvesterPlanting>(rpc.harvester, false);

            ecb.DestroyEntity(entity);
        }
        //Defuse Start
        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcHarvesterDefuseStart rpc, Entity entity) in SystemAPI
            .Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterDefuseStart>()
            .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);

            Entity character = rpc.character;

            if (character == Entity.Null)
            {
                Debug.LogError("Couldn't find character linked to rpc");
                continue;
            }

            if (defuserEntity != Entity.Null)
            {
                Debug.Log("Someone else is defusing!");
                continue;
            }

            if (currentTick.TicksSince(rpc.defuseStartTick) > 15)
            {
                Debug.Log("Time difference too great, switching to server's current tick.");
                defuseStartTick = currentTick;
            }
            else
            {
                defuseStartTick = rpc.defuseStartTick;
            }

            defuserEntity = character;
            defusingHarvesterEntity = rpc.harvester;
        }
        //Defuse Stop
        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcHarvesterDefuseStop rpc, Entity entity) in SystemAPI
            .Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterDefuseStop>()
            .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);

            Entity character = rpc.character;
            if (defuserEntity == Entity.Null)
            {
                Debug.Log("Nobody is defusing already");
                continue; 
            }

            if (defuserEntity != character)
            {
                Debug.Log("The defuser is someone else");
                continue;
            }

            if (defusingHarvesterEntity != rpc.harvester)
            {
                Debug.Log("Trying to defuse the wrong harvester.");
                continue;
            }

            defuserEntity = Entity.Null;
            defusingHarvesterEntity = Entity.Null;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
