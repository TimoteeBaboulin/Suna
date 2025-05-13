using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

//[UpdateAfter(typeof(StuffSystemClient))] //Cause des problémes sur le build log car les 2 systemes ne font pas partit du même groupe:
//Ignoring invalid[Unity.Entities.UpdateAfterAttribute] attribute on HarvesterSystemClient targeting StuffSystemClient.
//This attribute can only order systems that are members of the same ComponentSystemGroup instance.

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class HarvesterSystemClient : SystemBase
{
    private DefaultInputSystem input;

    DefaultInputSystem.PlayerActions playerActions;
    DefaultInputSystem.HarvesterActions harvesterActions;
    bool firstFrame;

    protected override void OnCreate()
    {
        input = World.GetOrCreateSystemManaged<CharacterInputSystem>().input;
        input.Enable();

        playerActions = input.Player;
        harvesterActions = input.Harvester;

        RequireForUpdate<EquipStuffQueue>();
        RequireForUpdate<UnequipStuffQueue>();
    }

    private void AskForOwner(ref EntityCommandBuffer ecb)
    {
        Entity rpcEntity = ecb.CreateEntity();
        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
        ecb.AddComponent<RpcRequestHarvesterOwners>(rpcEntity);

        firstFrame = true;
    }
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        if (firstFrame)
        {
            AskForOwner(ref ecb);
            firstFrame = false;
        }

        EntityQuery clientQuery = Entities.WithAll<GhostOwnerIsLocal, ClientComponent>().ToQuery();
        NativeArray<Entity> clientEntities = clientQuery.ToEntityArray(Allocator.Temp);
        if (clientEntities.Length is 0)
            return;

        Entity clientEntity = clientEntities[0];
        Entity localCharacter = SystemAPI.GetComponentRO<ClientCharacterAttached>(clientEntity).ValueRO.Value;

        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.ServerTick;

        var equipStuffQueu = SystemAPI.GetSingletonBuffer<EquipStuffQueue>();

        foreach ((RefRW<HarvesterComponent> harvesterRW, RefRW<StuffDynamicData> ownerRW, Entity harvesterEntity) in SystemAPI
            .Query<RefRW<HarvesterComponent>, RefRW<StuffDynamicData>>()
            .WithEntityAccess())
        {
            if (!EntityManager.IsComponentEnabled<IsStuffInHand>(harvesterEntity))
                continue;

            if (ownerRW.ValueRO.owner != localCharacter)
                continue;

            Entity characterEntity = SystemAPI.GetComponentRO<ClientCharacterAttached>(clientEntity).ValueRO.Value;

            if (playerActions.Attack.WasPressedThisFrame())
            {
                RpcHarvesterPlantStart rpc = new RpcHarvesterPlantStart
                {
                    tick = currentTick,
                    harvester = harvesterEntity,
                    character = characterEntity
                };

                Entity rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, rpc);
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
            }

            if (harvesterActions.Attack.WasReleasedThisFrame())
            {
                RpcHarvesterPlantStop rpc = new RpcHarvesterPlantStop
                {
                    tick = currentTick,
                    harvester = harvesterEntity
                };

                Entity rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, rpc);
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
            }
        }

        foreach ((RefRW<HarvesterComponent> HarvesterRW, LocalTransform harvesterTransform, Entity harvesterEntity) in SystemAPI
            .Query<RefRW<HarvesterComponent>, LocalTransform>()
            .WithAll<HarvesterPlanted>()
            .WithEntityAccess())
        {
            if (!EntityManager.HasComponent<StuffGameObjectRef>(harvesterEntity))
                continue;
            StuffGameObjectRef goRef = EntityManager.GetComponentObject<StuffGameObjectRef>(harvesterEntity);

            Entity characterEntity = SystemAPI.GetComponentRO<ClientCharacterAttached>(clientEntity).ValueRO.Value;
            float3 harvesterPos = harvesterTransform.Position;
            float3 characterPos = SystemAPI.GetComponentRO<LocalTransform>(characterEntity).ValueRO.Position;

            if (playerActions.Interact.WasPressedThisFrame() && math.distance(harvesterPos, characterPos) <= 10)
            {
                RpcHarvesterDefuseStart rpc = new RpcHarvesterDefuseStart
                {
                    harvester = harvesterEntity,
                    defuseStartTick = currentTick,
                    character = characterEntity
                };

                RpcUtils.SendClientToServerRpc(ref rpc);
            }

            //Find a way to check whethere we're currently defusing
            if (harvesterActions.Interact.WasReleasedThisFrame() || math.distance(harvesterPos, characterPos) >= 10)
            {
                RpcHarvesterDefuseStop rpc = new RpcHarvesterDefuseStop
                {
                    harvester = harvesterEntity,
                    defuseStopTick = currentTick,
                    character = characterEntity
                };

                RpcUtils.SendClientToServerRpc(ref rpc);
            }
        }

        //HARVESTER PLANTED
        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcHarvesterPlanted rpc, Entity entity)
             in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterPlanted>().WithEntityAccess())
        {
            PlantHarvester(rpc, ecb);

            ecb.DestroyEntity(entity);
        }

        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcHarvesterDefused rpc, Entity rpcEntity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterDefused>()
            .WithEntityAccess())
        {
            DefuseHarvester(rpc.harvester);
            ecb.DestroyEntity(rpcEntity);
        }

        //HARVESTER PICKED UP
        //foreach ((RefRO<ReceiveRpcCommandRequest> RequestSceneLoaded, RpcHarvesterOwnerChange rpc, Entity entity)
        //    in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterOwnerChange>().WithEntityAccess())
        //{
        //    ecb.DestroyEntity(entity);

        //    if (rpc.harvester == Entity.Null)
        //    {
        //        Entity responseEntity = ecb.CreateEntity();
        //        ecb.AddComponent<RpcRequestHarvesterOwners>(responseEntity);
        //        ecb.AddComponent<SendRpcCommandRequest>(responseEntity);

        //        continue;
        //    }

        //    if (SystemAPI.HasComponent<StuffDynamicData>(rpc.harvester))
        //    {
        //        StuffDynamicData stuffOwner = SystemAPI.GetComponent<StuffDynamicData>(rpc.harvester);

        //        if (stuffOwner.owner != Entity.Null)
        //        {
        //            StuffUtils.UnequipNextFrame(unequipStuffQueu, stuffOwner.owner, rpc.harvester);
        //        }
        //    }

        //    StuffUtils.EquipNextFrame(equipStuffQueu, rpc.character, rpc.harvester, true);
        //}

        foreach ((RefRO<ReceiveRpcCommandRequest> RequestSceneLoaded, RpcHarvesterDropped rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterDropped>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);

            if (rpc.harvester == Entity.Null)
            {
                RpcRequestHarvesterOwners response = new();
                RpcUtils.SendClientToServerRpc(ref response);

                continue;
            }

            SystemAPI.GetComponentRW<LocalTransform>(rpc.harvester).ValueRW.Position = rpc.position;
            ChangeHarvesterOwner(rpc.harvester, Entity.Null);

            //if (SystemAPI.HasComponent<StuffDynamicData>(rpc.harvester))
            //{
            //    StuffDynamicData stuffOwner = SystemAPI.GetComponent<StuffDynamicData>(rpc.harvester);

            //    if (stuffOwner.owner != Entity.Null)
            //    {
            //        StuffUtils.UnequipNextFrame(unequipStuffQueu, stuffOwner.owner, rpc.harvester);
            //    }
            //    else
            //    {
            //        SystemAPI.GetComponentRW<LocalTransform>(rpc.harvester).ValueRW.Position = rpc.position;
            //        EntityManager.GetComponentObject<StuffGameObjectRef>(rpc.harvester).Value.transform.position = rpc.position;
            //    }
            //}
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private void PlantHarvester(RpcHarvesterPlanted rpc, EntityCommandBuffer ecb)
    {
        Entity harvesterEntity = rpc.harvester;
        Entity ownerEntity = rpc.harvesterOwner;
        NetworkTick plantTick = rpc.plantedTick;
        float3 plantPosition = rpc.plantPosition;
        LocalTransform harvesterTransform = SystemAPI.GetComponent<LocalTransform>(harvesterEntity);

        //ecb.SetComponentEnabled<HarvesterPlanted>(harvesterEntity, true);

        ChangeHarvesterOwner(harvesterEntity, Entity.Null);

        harvesterTransform.Position = plantPosition;
        harvesterTransform.Rotation = quaternion.identity;

        StuffGameObjectRef goRef = World.EntityManager.GetComponentObject<StuffGameObjectRef>(harvesterEntity);
        goRef.Value.GetComponent<HarvesterVfxLink>().Play();
        StuffUtils.SetStuffViewTransform(goRef, harvesterTransform);

        SystemAPI.SetComponent(harvesterEntity, harvesterTransform);

        //Debug.Log($"[Harvester] Client planted harvester {harvesterEntity}");
    }

    private void DefuseHarvester(Entity harvesterEntity)
    {
        StuffGameObjectRef goRef = World.EntityManager.GetComponentObject<StuffGameObjectRef>(harvesterEntity);
        goRef.Value.GetComponent<HarvesterVfxLink>().Stop();

        //Debug.Log("[Harvester] Client received defuse rpc");
    }

    private void ChangeHarvesterOwner(Entity harvesterEntity, Entity newOwner)
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

            StuffUtils.Unequip(ref CheckedStateRef, oldOwner, linkedEntityGroup, ownerStuffList,
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
