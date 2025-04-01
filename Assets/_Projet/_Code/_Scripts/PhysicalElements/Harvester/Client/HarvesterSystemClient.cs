using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(StuffSystemClient))]
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

        RequireForUpdate<EquipStuffQueu>();
        RequireForUpdate<UnequipStuffQueu>();
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

        var equipStuffQueu = SystemAPI.GetSingletonBuffer<EquipStuffQueu>();
        var unequipStuffQueu = SystemAPI.GetSingletonBuffer<UnequipStuffQueu>();

        foreach ((RefRW<HarvesterComponent> harvesterRW, RefRW<StuffOwner> ownerRW, Entity harvesterEntity) in SystemAPI
            .Query<RefRW<HarvesterComponent>, RefRW<StuffOwner>>()
            .WithEntityAccess())
        {
            if (!EntityManager.IsComponentEnabled<IsStuffInHand>(harvesterEntity))
                continue;

            if (ownerRW.ValueRO.Value != localCharacter)
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
            else if (harvesterActions.Attack.WasReleasedThisFrame())
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

                Entity rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, rpc);
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
            }

            //Find a way to check whethere we're currently defusing
            if (harvesterActions.Interact.WasReleasedThisFrame())
            {
                RpcHarvesterDefuseStop rpc = new RpcHarvesterDefuseStop
                {
                    harvester = harvesterEntity,
                    defuseStopTick = currentTick,
                    character = characterEntity
                };

                Entity rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, rpc);
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
            }
        }

        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcHarvesterPlanted rpc, Entity entity)
             in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterPlanted>().WithEntityAccess())
        {
            ecb.SetComponentEnabled<HarvesterPlanted>(rpc.harvester, true);

            unequipStuffQueu.Add(new UnequipStuffQueu
            {
                Stuff = rpc.harvester,
                Owner = rpc.harvesterOwner,
                Position = rpc.plantPosition
            });

            ecb.DestroyEntity(entity);
        }

        //Add Harvester to Player
        foreach ((RefRO<ReceiveRpcCommandRequest> RequestSceneLoaded, RpcHarvesterOwnerChange rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterOwnerChange>().WithEntityAccess())
        {
            //ecb.RemoveComponent<TemporaryOverrideGameObjectActive>(rpc.harvester);

            //StuffGameObjectRef goRef = EntityManager.GetComponentObject<StuffGameObjectRef>(rpc.harvester);
            //CommonCharacterModelBonesTransform charaBones = EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(rpc.character);
            //StuffCommonData commonData = EntityManager.GetSharedComponent<StuffCommonData>(rpc.harvester);

            //goRef.Value.transform.SetParent(charaBones.WeaponSlotTransform);
            //goRef.Value.transform.localPosition = commonData._stuffLocalOffsetView;

            //SystemAPI.GetComponentRW<HarvesterComponent>(rpc.harvester).ValueRW.Owner = rpc.newOwner;

            ecb.DestroyEntity(entity);

            if (rpc.character == Entity.Null)
            {
                Entity responseEntity = ecb.CreateEntity();
                ecb.AddComponent<RpcRequestHarvesterOwners>(responseEntity);
                ecb.AddComponent<SendRpcCommandRequest>(responseEntity);

                continue;
            }

            equipStuffQueu.Add(new EquipStuffQueu
            {
                Stuff = rpc.harvester,
                Owner = rpc.character
            });

            
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
