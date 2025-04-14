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
        var equipStuffQueue = SystemAPI.GetSingletonBuffer<EquipStuffQueue>();

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

                UnityEngine.Debug.Log($"Forward: {forward}");
                RaycastHit hit = ClosestRayCast(startPosition, forward, 1, chara, state.EntityManager);
                UnityEngine.Debug.Log($"Hit: {hit.Entity} {hit.Position}");
                if (!state.EntityManager.HasComponent<StuffOwner>(hit.Entity)) continue;
                UnityEngine.Debug.Log("Success !");

                equipStuffQueue.Add(new EquipStuffQueue
                {
                    Stuff = hit.Entity,
                    Owner = chara,
                });
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
        //    }
        //}
    }

    RaycastHit ClosestRayCast(float3 startPos, float3 direction, float range, Entity owner, in EntityManager entityManager)
    {

        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        RaycastHit closestHit = default;

        RaycastInput raycastInput = new RaycastInput()
        {
            Start = startPos,
            End = direction * range,
        };

        NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
        if (physicsWorldSingleton.CastRay(raycastInput, ref allHits))
        {
            float closestDist = float.MaxValue;
            foreach (RaycastHit hit in allHits)
            {
                // If the entity hit is the shooter, skip
                if (hit.Entity == owner) continue;

                // If the entity hit have an owner, skip
                if (entityManager.HasComponent<StuffOwner>(hit.Entity))
                {
                    Entity ownerOfStuffHited = entityManager.GetComponentData<StuffOwner>(hit.Entity).Value;
                    if (ownerOfStuffHited != Entity.Null)
                    {
                        continue;
                    }
                }

                float currentDist = math.distancesq(raycastInput.Start, hit.Position);

                if (currentDist < closestDist)
                {
                    closestHit = hit;
                    closestDist = currentDist;
                }
            }
        }

#if !UNITY_SERVER
        UnityEngine.Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, UnityEngine.Color.cyan, 0.5f);
#endif
        return closestHit;
    }
}
