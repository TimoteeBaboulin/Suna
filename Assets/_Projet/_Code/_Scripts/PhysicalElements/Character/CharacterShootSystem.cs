using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using RaycastHit = Unity.Physics.RaycastHit;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct HasHitComponent : IComponentData
{
    [GhostField] public bool Value;
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct ShootSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<NetworkTime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        if (!networkTime.IsFirstPredictionTick)
        {
            return;
        }

        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (transform, input, hasHit, entity) in SystemAPI
            .Query<RefRO<LocalTransform>, RefRO<CharacterInput>, RefRW<HasHitComponent>>()
            .WithAll<Simulate>()
            .WithEntityAccess())
        {
            if (!input.ValueRO.shoot.IsSet)
            {
                if (state.World.IsServer())
                {
                    hasHit.ValueRW.Value = false;
                }

                continue;
            }
            
            float3 startPosition = input.ValueRO.shootTransform.Position;
            float3 endPosition = startPosition + new float3(input.ValueRO.shootTransform.Forward() * 100);

            RaycastInput raycastInput = new RaycastInput()
            {
                Start = startPosition,
                End = endPosition,
                Filter = CollisionFilter.Default
            };

            NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
            if (physicsWorldSingleton.CastRay(raycastInput, ref allHits))
            {
                RaycastHit closestHit;

                if (allHits[0].Entity == entity
                    && allHits.Length > 1)
                {
                    closestHit = allHits[1];
                }
                else
                {
                    closestHit = allHits[0];
                }

                float closestDistance = math.distancesq(raycastInput.Start, closestHit.Position);

                foreach (var hit in allHits)
                {
                    if (hit.Entity == entity)
                    {
                        continue;
                    }

                    float distance = math.distancesq(raycastInput.Start, hit.Position);

                    if (distance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = distance;
                    }
                }

                if (state.World.IsServer()
                    && closestHit.Entity != entity
                    && state.EntityManager.HasComponent<DamageBufferElement>(closestHit.Entity))
                {
                    ecb.AppendToBuffer(closestHit.Entity, new DamageBufferElement { Value = 10 });
                    ecb.SetComponent(entity, new HasHitComponent { Value = true });
                }
            }

            Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.red, 0.5f);
        }
    }
}
