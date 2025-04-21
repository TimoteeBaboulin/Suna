using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using static UnityEngine.UI.GridLayoutGroup;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PickStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterStuffList>();
        state.RequireForUpdate<CharacterInput>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Get ECB
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var equipStuffQueue = SystemAPI.GetSingletonBuffer<EquipStuffQueue>();

        //Pick stuff with interact input
        foreach (var (inputRO, shootStartPosDeltaRO, transformRO, viewRO, chara) in SystemAPI
        .Query<RefRO<CharacterInput>, RefRO<CharacterShootStartPositionDelta>, RefRO<LocalTransform>, RefRO<CharacterViewRotation>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRO.ValueRO;
            if (input.interact.IsSet)
            {
                float3 startPosition = shootStartPosDeltaRO.ValueRO.PositionDelta + transformRO.ValueRO.Position;
                quaternion shootRotation = math.mul(transformRO.ValueRO.Rotation, viewRO.ValueRO.ViewRotation);
                float3 forward = math.mul(shootRotation, math.forward());

                RaycastHit hit = RayCast(startPosition, forward, 4, chara, state.EntityManager);
                if (hit.Entity != Entity.Null)
                {
                    StuffUtils.EquipNextFrame(equipStuffQueue, chara, state.EntityManager.GetComponentData<StuffEntityInHandRef>(hit.Entity).Value, true);

                    ecb.DestroyEntity(hit.Entity);
                }
            }
        }

        //foreach (var (stuffListRW, inputRO, chara) in SystemAPI
        //.Query<RefRW<CharacterStuffList>, RefRO<CharacterInput>>()
        //.WithEntityAccess())
        //{
        //    ref readonly CharacterInput input = ref inputRO.ValueRO;
        //    ref CharacterStuffList stuffList = ref stuffListRW.ValueRW;
        //    if (input.interact.IsSet)
        //    {
        //        equipStuffQueue.Add(new EquipStuffQueue
        //        {
        //            Stuff = stuffList.StuffInHand,
        //            Owner = chara,
        //        });

        //        ecb.DestroyEntity(hit.Entity);
        //        SystemAPI.GetComponentRW<StuffDynamicData>(hit.Entity).ValueRW.dropedEntity = Entity.Null;

        //    }
        //}
    }

    RaycastHit RayCast(float3 startPos, float3 direction, float range, Entity owner, in EntityManager entityManager)
    {
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        RaycastInput raycastInput = new RaycastInput()
        {
            Start = startPos,
            End = startPos + direction * range,
            Filter = CollisionFilter.Default,
        };

#if !UNITY_SERVER
        UnityEngine.Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, UnityEngine.Color.cyan, 0.5f);
#endif

        NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
        if (physicsWorldSingleton.CastRay(raycastInput, ref allHits))
        {
            foreach (RaycastHit hit in allHits)
            {
                if (entityManager.HasComponent<StuffEntityInHandRef>(hit.Entity))
                {
                    return hit;
                }
            }
        }
        return default;
    }
}
