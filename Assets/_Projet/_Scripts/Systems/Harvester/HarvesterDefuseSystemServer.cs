using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(HarvesterSystemServer))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct HarvesterDefuseSystemServer : ISystem
{
    Entity defuserEntity;
    Entity defusingHarvesterEntity;
    NetworkTick defuseStartTick;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        defuserEntity = Entity.Null;
        defusingHarvesterEntity = Entity.Null;

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<HarvesterComponent>().Build(ref state);
        state.RequireForUpdate(query);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        //Prepare the current tick since it's used in multiple branches
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out NetworkTime networkTime))
        {
            return;
        }
        NetworkTick currentTick = networkTime.InterpolationTick;

        RoundPhase currentPhase;

        if (SystemAPI.TryGetSingleton<RoundComponent>(out var roundComponent))
        {
            currentPhase = roundComponent.currentPhase;
        }
        else
        {
            Debug.LogError("[Server] Couldn't find round component for harvester defuse systems");
            currentPhase = RoundPhase.ActionPhase;
        }

        if (defusingHarvesterEntity != Entity.Null)
        {
            if (currentTick.TicksSince(defuseStartTick) < 4 * 60)
            {
                float3 harvesterPos = SystemAPI.GetComponentRO<LocalTransform>(defusingHarvesterEntity).ValueRO.Position;
                float defuseRange = SystemAPI.GetComponentRO<HarvesterComponent>(defusingHarvesterEntity).ValueRO.defuseRange;
                float3 defuserPos = SystemAPI.GetComponentRO<LocalTransform>(defuserEntity).ValueRO.Position;

                if (math.distance(harvesterPos, defuserPos) > defuseRange)
                {
                    defusingHarvesterEntity = Entity.Null;
                    defuserEntity = Entity.Null;

                    Debug.Log("[Server] Stopped defusing because of distance");
                    SystemAPI.GetComponentRW<PlayerHarvesterActions>(defuserEntity).ValueRW.IsDefusing = false;
                }
            }
            else
            {
                //Defused
                SystemAPI.SetComponentEnabled<HarvesterPlanted>(defusingHarvesterEntity, false);
                SystemAPI.GetComponentRW<PlayerHarvesterActions>(defuserEntity).ValueRW.IsDefusing = false;
                SystemAPI.SetComponentEnabled<HarvesterDefusing>(defusingHarvesterEntity, false);

                defusingHarvesterEntity = Entity.Null;
                defuserEntity = Entity.Null;
                Debug.Log("[Server] Harvester was defused");
            }
        }



        //RPC HANDLING____________________________________________________________________________________
        //Defuse Start
        foreach ((RefRO<ReceiveRpcCommandRequest> request, RpcHarvesterDefuseStart rpc, Entity entity) in SystemAPI
            .Query<RefRO<ReceiveRpcCommandRequest>, RpcHarvesterDefuseStart>()
            .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);

            Entity character = rpc.character;

            if (character == Entity.Null)
            {
                Debug.LogError("[Server] Couldn't find character linked to rpc");
                continue;
            }

            if (defuserEntity != Entity.Null)
            {
                Debug.Log("[Server] Someone else is defusing!");
                continue;
            }

            if (!SystemAPI.HasComponent<NatifTeamTag>(rpc.character))
            {
                Debug.Log("[Server] Corpo player trying to defuse");
                continue;
            }

            if (currentTick.TicksSince(rpc.defuseStartTick) > 15)
            {
                Debug.Log("[Server] Time difference too great, switching to server's current tick.");
                defuseStartTick = currentTick;
            }
            else
            {
                defuseStartTick = rpc.defuseStartTick;
            }

            Debug.Log("[Server] Defuse started");

            SystemAPI.SetComponentEnabled<HarvesterDefusing>(rpc.harvester, true);
            SystemAPI.GetComponentRW<HarvesterDefusing>(rpc.harvester).ValueRW.DefuseStartedTick = defuseStartTick;
            SystemAPI.GetComponentRW<PlayerHarvesterActions>(rpc.character).ValueRW.IsDefusing = true;
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
                Debug.Log("[Server] Nobody is defusing already");
                continue;
            }

            if (defuserEntity != character)
            {
                Debug.Log("[Server] The defuser is someone else");
                continue;
            }

            if (defusingHarvesterEntity != rpc.harvester)
            {
                Debug.Log("[Server] Trying to defuse the wrong harvester.");
                continue;
            }

            Debug.Log("[Server] Defuse stopped");

            SystemAPI.SetComponentEnabled<HarvesterDefusing>(rpc.harvester, false);
            SystemAPI.GetComponentRW<PlayerHarvesterActions>(defuserEntity).ValueRW.IsDefusing = false;
            defuserEntity = Entity.Null;
            defusingHarvesterEntity = Entity.Null;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
