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
                        SystemAPI.Query<RefRW<HarvesterComponent>, RefRW<LocalTransform>, RefRW<StuffDynamicData>>()
                        .WithAll<HarvesterPlanting>()
                        .WithEntityAccess())
            {
                NetworkTick plantStartTick = SystemAPI.GetComponentRO<HarvesterPlanting>(harvesterEntity).ValueRO.PlantStartedTick;
                if (currentTick.TicksSince(plantStartTick) >= 60 * 4)
                {
                    SystemAPI.SetComponentEnabled<HarvesterPlanting>(harvesterEntity, false);
                    ecb.SetComponentEnabled<HarvesterPlanted>(harvesterEntity, true);
                    Entity characterEntity = ownerRW.ValueRO.owner;

                    //StuffSlot switchToLocation = StuffSlot.MainWeapon;
                    //Entity targetWeaponEntity = Entity.Null;
                    //DynamicBuffer<CharacterStuffList> stuffList = SystemAPI.GetBuffer<CharacterStuffList>(characterEntity);
                    //if (StuffUtils.GetStuffInSlot(stuffList, StuffSlot.MainWeapon) == Entity.Null)
                    //{
                    //    if (StuffUtils.GetStuffInSlot(stuffList, StuffSlot.SecondaryWeapon) == Entity.Null)
                    //    {
                    //        switchToLocation = StuffSlot.Melee;
                    //    }
                    //    else
                    //    {
                    //        switchToLocation = StuffSlot.Melee;
                    //    }
                    //}
                    //targetWeaponEntity = StuffUtils.GetStuffInSlot(stuffList, switchToLocation);

                    //SystemAPI.GetComponentRW<CharacterStuffInfos>(characterEntity).ValueRW.StuffInHandSlot = StuffSlot.Melee;

                    PlantHarvester(ref state, harvesterEntity);

                    float3 plantPosition = SystemAPI.GetComponentRO<LocalTransform>(characterEntity).ValueRO.Position - new float3(0, 0.75f, 0);
                    harvesterTransformRW.ValueRW.Position = plantPosition;
                    SystemAPI.GetComponentRW<HarvesterPlanting>(harvesterEntity).ValueRW.PlantStartedTick = currentTick;

                    RpcHarvesterPlanted rpc = new RpcHarvesterPlanted
                    {
                        harvester = harvesterEntity,
                        plantedTick = currentTick,
                        harvesterOwner = characterEntity,
                        plantPosition = plantPosition
                    };

                    EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<ClientComponent>().Build(ref state);

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

            if (!SystemAPI.HasComponent<CorpoTeamTag>(rpc.character))
            {
                Debug.Log("[Server] A native is trying to plant the defuser");

                continue;
            }

            ecb.SetComponentEnabled<HarvesterPlanting>(rpc.harvester, true);

            if (currentTick.TicksSince(rpc.tick) > 10)
            {
                Debug.Log("[Server] Tick difference too great, using the server's current tick");
                SystemAPI.GetComponentRW<HarvesterPlanting>(rpc.harvester).ValueRW.PlantStartedTick = currentTick;
            }
            else
            {
                SystemAPI.GetComponentRW<HarvesterPlanting>(rpc.harvester).ValueRW.PlantStartedTick = rpc.tick;
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

            Entity owner = SystemAPI.GetComponentRO<StuffDynamicData>(rpc.harvester).ValueRO.owner;
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

    private void PlantHarvester(ref SystemState state, Entity harvester)
    {
        ChangeHarvesterOwner(ref state, harvester, Entity.Null);
    }

    private void ChangeHarvesterOwner(ref SystemState state, Entity harvesterEntity, Entity newOwner)
    {
        Entity oldOwner = SystemAPI.GetComponent<StuffDynamicData>(harvesterEntity).owner;
        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();

        if (oldOwner != Entity.Null)
        {
            DynamicBuffer<UnequipStuffQueue> unequipQueue = SystemAPI.GetSingletonBuffer<UnequipStuffQueue>();

            var linkedEntityGroup = SystemAPI.GetBuffer<LinkedEntityGroup>(oldOwner);
            var ownerStuffList = SystemAPI.GetBuffer<CharacterStuffList>(oldOwner);

            //Stuff
            var stuffGhostOwnerRW = SystemAPI.GetComponentRW<GhostOwner>(harvesterEntity);
            var stuffDynamicDataRW = SystemAPI.GetComponentRW<StuffDynamicData>(harvesterEntity);
            ref var stuffData = ref SystemAPI.GetComponentRO<StuffDatabaseAccess>(harvesterEntity).ValueRO.GetData(ref database);

            StuffUtils.Unequip(ref state, oldOwner, linkedEntityGroup, ownerStuffList,
                harvesterEntity, stuffGhostOwnerRW, ref stuffData);
            stuffDynamicDataRW.ValueRW.owner = Entity.Null;

            //StuffUtils.UnequipNextFrame(unequipQueue, oldOwner, harvesterEntity);
        }

        if (newOwner != Entity.Null)
        {
            DynamicBuffer<EquipStuffQueue> equipQueue = SystemAPI.GetSingletonBuffer<EquipStuffQueue>();

            StuffUtils.EquipNextFrame(equipQueue, newOwner, harvesterEntity, false);
        }
    }
}
