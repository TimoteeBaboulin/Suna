using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using RaycastHit = Unity.Physics.RaycastHit;

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

        foreach (var (transform, shootInput, cameraAttach, entity) in SystemAPI
            .Query<RefRO<LocalTransform>, RefRO<PlayerInput>, RefRO<CameraAttachComponent>>()
            .WithAll<HasHitComponent, Simulate>()
            .WithEntityAccess())
        {
            if (!shootInput.ValueRO.shoot.IsSet)
            {
                ecb.SetComponent(entity, new HasHitComponent { Value = false });
                continue;
            }

            float3 startPosition = cameraAttach.ValueRO.transform.Position;
            float3 endPosition = startPosition + (cameraAttach.ValueRO.transform.Forward() * 100);

            RaycastInput raycastInput = new RaycastInput()
            {
                Start = startPosition,
                End = endPosition,
                Filter = CollisionFilter.Default
            };

            NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
            if (physicsWorldSingleton.CastRay(raycastInput, ref allHits))
            {
                foreach (var hit in allHits)
                {
                    if (hit.Entity == entity)
                    {
                        continue;
                    }

                    if (state.EntityManager.HasComponent<DamageBufferElement>(hit.Entity))
                    {
                        if (state.World.IsServer())
                        {
                            ecb.AppendToBuffer(hit.Entity, new DamageBufferElement { Value = 10 });
                        }

                        ecb.SetComponent(entity, new HasHitComponent { Value = true });
                    }

                    break;
                }
            }

            Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.red, 1);
        }
    }
}
