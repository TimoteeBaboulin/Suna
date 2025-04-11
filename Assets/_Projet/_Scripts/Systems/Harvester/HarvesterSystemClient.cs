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
        var unequipStuffQueu = SystemAPI.GetSingletonBuffer<UnequipStuffQueue>();

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

            unequipStuffQueu.Add(new UnequipStuffQueue
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
            ecb.DestroyEntity(entity);

            if (rpc.harvester == Entity.Null)
            {
                Entity responseEntity = ecb.CreateEntity();
                ecb.AddComponent<RpcRequestHarvesterOwners>(responseEntity);
                ecb.AddComponent<SendRpcCommandRequest>(responseEntity);

                continue;
            }

            if (SystemAPI.HasComponent<StuffOwner>(rpc.harvester))
            {
                StuffOwner stuffOwner = SystemAPI.GetComponent<StuffOwner>(rpc.harvester);

                if (stuffOwner.Value != Entity.Null)
                {
                    unequipStuffQueu.Add(new UnequipStuffQueue
                    {
                        Stuff = rpc.harvester,
                        Owner = stuffOwner.Value
                    });
                }
            }

            equipStuffQueu.Add(new EquipStuffQueue
            {
                Stuff = rpc.harvester,
                Owner = rpc.character
            });
        }

        foreach ((RefRO<ReceiveRpcCommandRequest> RequestSceneLoaded, RpcHarvesterDropped rpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterDropped>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
            Debug.Log("RPC Harvester Dropped message received");

            if (rpc.harvester == Entity.Null)
            {
                Entity responseEntity = ecb.CreateEntity();
                ecb.AddComponent<RpcRequestHarvesterOwners>(responseEntity);
                ecb.AddComponent<SendRpcCommandRequest>(responseEntity);

                continue;
            }

            if (SystemAPI.HasComponent<StuffOwner>(rpc.harvester))
            {
                StuffOwner stuffOwner = SystemAPI.GetComponent<StuffOwner>(rpc.harvester);

                if (stuffOwner.Value != Entity.Null)
                {
                    unequipStuffQueu.Add(new UnequipStuffQueue
                    {
                        Stuff = rpc.harvester,
                        Owner = stuffOwner.Value,
                        Position = rpc.position
                    });
                }
                else
                {
                    SystemAPI.GetComponentRW<LocalTransform>(rpc.harvester).ValueRW.Position = rpc.position;
                    EntityManager.GetComponentObject<StuffGameObjectRef>(rpc.harvester).Value.transform.position = rpc.position;
                }
            }
        }

            ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
