using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(StuffSystems))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class HarvesterSystemClient : SystemBase
{
    private DefaultInputSystem input;
    DefaultInputSystem.PlayerActions actions;
    bool firstFrame;

    protected override void OnCreate()
    {
        
        input = new DefaultInputSystem();
        input.Enable();
        actions = input.Player;
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

        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.ServerTick;

        foreach ((RefRW<HarvesterComponent> harvesterRW, Entity harvesterEntity) in SystemAPI
            .Query<RefRW<HarvesterComponent>>()
            .WithEntityAccess())
        {
            if (!EntityManager.HasComponent<StuffGameObjectRef>(harvesterEntity))
            {
                //Debug.Log("Instanciating harvester GO");
                StuffGameObjectPrefab prefabRef = EntityManager.GetComponentObject<StuffGameObjectPrefab>(harvesterEntity);
                StuffGameObjectRef goRef = new StuffGameObjectRef
                {
                    Value = Object.Instantiate(prefabRef.Value)
                };

                ecb.AddComponent<TemporaryOverrideGameObjectActive>(harvesterEntity);
                goRef.Value.transform.position = SystemAPI.GetComponentRO<LocalTransform>(harvesterEntity).ValueRO.Position;
                ecb.AddComponent(harvesterEntity, goRef);
            }
            else if (harvesterRW.ValueRO.Owner == Entity.Null)
            {
                StuffGameObjectRef goRef = EntityManager.GetComponentObject<StuffGameObjectRef>(harvesterEntity);
                goRef.Value.transform.position = SystemAPI.GetComponentRO<LocalTransform>(harvesterEntity).ValueRO.Position;
            }

            if (!EntityManager.IsComponentEnabled<IsStuffInHand>(harvesterEntity))
                continue;
            
            if (harvesterRW.ValueRO.Owner != clientEntity)
                continue;

            Entity characterEntity = SystemAPI.GetComponentRO<ClientCharacterAttached>(clientEntity).ValueRO.Value;

            if (actions.Attack.WasPressedThisFrame())
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
            else if (actions.Attack.WasReleasedThisFrame())
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

        foreach((RefRW<HarvesterComponent> HarvesterRW, LocalTransform harvesterTransform, Entity harvesterEntity) in SystemAPI
            .Query<RefRW<HarvesterComponent>, LocalTransform>()
            .WithAll<HarvesterPlanted>()
            .WithEntityAccess())
        {
            Entity characterEntity = SystemAPI.GetComponentRO<ClientCharacterAttached>(clientEntity).ValueRO.Value;
            float3 harvesterPos = harvesterTransform.Position;
            float3 characterPos = SystemAPI.GetComponentRO<LocalTransform>(characterEntity).ValueRO.Position;

            if (actions.Interact.WasPressedThisFrame() && math.distance(harvesterPos, characterPos) <= 10)
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
            if (actions.Interact.WasReleasedThisFrame())
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
            SystemAPI.GetComponentRW<CharacterStuffList>(rpc.harvesterOwner).ValueRW.Value[(int)StuffType.Harvester] = Entity.Null;
            ecb.SetComponentEnabled<HarvesterPlanted>(rpc.harvester, true);

            StuffGameObjectRef goRef = EntityManager.GetComponentObject<StuffGameObjectRef>(rpc.harvester);
            if (goRef is null)
                return;

            goRef.Value.transform.SetParent(null);
            goRef.Value.transform.position = rpc.plantPosition;
            goRef.Value.SetActive(true);

            ecb.AddComponent<TemporaryOverrideGameObjectActive>(rpc.harvester);
            ecb.DestroyEntity(entity);
        }

        foreach ((RefRO<ReceiveRpcCommandRequest> RequestSceneLoaded, RpcHarvesterOwnerChange rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterOwnerChange>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
            if (rpc.character == Entity.Null)
            {
                Debug.Log("[Client - Harvester] Couldn't change harvester owner due to the entity not being spawned yet. Asking for re-send from server.");

                AskForOwner(ref ecb);

                continue;
            }

            ecb.RemoveComponent<TemporaryOverrideGameObjectActive>(rpc.harvester);
            StuffGameObjectRef goRef = EntityManager.GetComponentObject<StuffGameObjectRef>(rpc.harvester);
            CommonCharacterModelBonesTransform charaBones = EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(rpc.character);
            StuffCommonData commonData = EntityManager.GetSharedComponent<StuffCommonData>(rpc.harvester);

            goRef.Value.transform.SetParent(charaBones.WeaponSlotTransform);
            goRef.Value.transform.localPosition = commonData._stuffLocalOffsetView;

            SystemAPI.GetComponentRW<HarvesterComponent>(rpc.harvester).ValueRW.Owner = rpc.newOwner;
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();

    }
}
