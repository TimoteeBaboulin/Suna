using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(HarvesterSystemServer))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct HarvesterPlantingSystemServer : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<HarvesterComponent>().Build(ref state);
        state.RequireForUpdate(query);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        //Prepare the current tick since it's used in multiple branches
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.InterpolationTick;

        RoundPhase currentPhase;

        if (SystemAPI.TryGetSingleton<RoundComponent>(out var roundComponent))
        {
            currentPhase = roundComponent.currentPhase;
        }
        else
        {
            Debug.LogError("[Server] Couldn't find round component for harvester planting systems");
            currentPhase = RoundPhase.ActionPhase;
        }

        if (currentPhase is RoundPhase.ActionPhase)
        {
            foreach (var (harvesterRW, harvesterTransformRW, ownerRW, harvesterEntity) in
                        SystemAPI.Query<RefRW<HarvesterComponent>, RefRW<LocalTransform>, RefRW<StuffOwner>> ()
                        .WithAll<HarvesterPlanting>()
                        .WithEntityAccess())
            {
                if (currentTick.TicksSince(harvesterRW.ValueRO.PlantStartedTick) >= 60 * 4)
                {
                    SystemAPI.SetComponentEnabled<HarvesterPlanting>(harvesterEntity, false);
                    ecb.SetComponentEnabled<HarvesterPlanted>(harvesterEntity, true);
                    Entity characterEntity = ownerRW.ValueRO.Value;

                    
                    StuffInventoryLocation switchToLocation = StuffInventoryLocation.MainWeapon;
                    Entity targetWeaponEntity = Entity.Null;
                    RefRW<CharacterStuffList> stuffListRW = SystemAPI.GetComponentRW<CharacterStuffList>(characterEntity);
                    if (stuffListRW.ValueRO.Value[(int)StuffInventoryLocation.MainWeapon] == Entity.Null)
                    {
                        if (stuffListRW.ValueRO.Value[(int)StuffInventoryLocation.SecondaryWeapon] == Entity.Null)
                        {
                            switchToLocation = StuffInventoryLocation.Melee;
                        }
                        else
                        {
                            switchToLocation = StuffInventoryLocation.Melee;
                        }
                    }
                    targetWeaponEntity = stuffListRW.ValueRO.Value[(int)switchToLocation];

                    SystemAPI.GetComponentRW<CharacterStuffInHandLocation>(characterEntity).ValueRW.Value = StuffInventoryLocation.Melee;
                    SystemAPI.SetComponentEnabled<IsStuffInHand>(targetWeaponEntity, true);

                    var unequipStuffQueu = SystemAPI.GetSingletonBuffer<UnequipStuffQueu>();
                    unequipStuffQueu.Add(new UnequipStuffQueu
                    {
                        Owner = characterEntity,
                        Stuff = harvesterEntity
                    });

                    float3 plantPosition = SystemAPI.GetComponentRO<LocalTransform>(characterEntity).ValueRO.Position - new float3(0, 0.75f, 0);
                    harvesterTransformRW.ValueRW.Position = plantPosition;
                    harvesterRW.ValueRW.PlantedTick = currentTick;

                    RpcHarvesterPlanted rpc = new RpcHarvesterPlanted
                    {
                        harvester = harvesterEntity,
                        plantedTick = currentTick,
                        harvesterOwner = characterEntity,
                        plantPosition = plantPosition
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
                    SystemAPI.GetComponentRW<PlayerHarvesterActions>(characterEntity).ValueRW.IsPlanting = false;
                    ownerRW.ValueRW.Value = Entity.Null;


                    Debug.Log("[Server] Harvester planted");
                }
            }
        }

        //RPC HANDLING____________________________________________________________________________________
        //Plant Start
        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcHarvesterPlantStart rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterPlantStart>().WithEntityAccess())
        {
            //Entity roundManagerEntity;
            ecb.DestroyEntity(entity);

            if (currentPhase is not RoundPhase.ActionPhase or RoundPhase.PostRoundPhase)
            {
                Debug.Log("[Server] Can't plant harvester during this phase");

                continue;
            }

            if (!SystemAPI.GetComponentRO<CharacterComponent>(rpc.character).ValueRO.isOnSite)
            {
                Debug.Log("[Server] Not on site");

                continue;
            }

            ecb.SetComponentEnabled<HarvesterPlanting>(rpc.harvester, true);

            if (currentTick.TicksSince(rpc.tick) > 10)
            {
                Debug.Log("[Server] Tick difference too great, using the server's current tick");
                SystemAPI.GetComponentRW<HarvesterComponent>(rpc.harvester).ValueRW.PlantStartedTick = currentTick;
            }
            else
            {
                SystemAPI.GetComponentRW<HarvesterComponent>(rpc.harvester).ValueRW.PlantStartedTick = rpc.tick;
            }
            SystemAPI.GetComponentRW<PlayerHarvesterActions>(rpc.character).ValueRW.IsPlanting = true;

            Debug.Log("[Server] Plant started");
        }
        //Plant Stop
        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcHarvesterPlantStop rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterPlantStop>().WithEntityAccess())
        {
            ecb.SetComponentEnabled<HarvesterPlanting>(rpc.harvester, false);

            Debug.Log("[Server] Plant stopped");

            Entity owner = SystemAPI.GetComponentRO<StuffOwner>(rpc.harvester).ValueRO.Value;
            SystemAPI.GetComponentRW<PlayerHarvesterActions>(owner).ValueRW.IsPlanting = false;
            ecb.DestroyEntity(entity);
        }

        foreach (var characterRW in SystemAPI.Query<RefRW<CharacterComponent>>())
        {
            characterRW.ValueRW.isOnSite = false;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
