using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

using RaycastHit = Unity.Physics.RaycastHit;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct ShootInputComponent : IInputComponentData
{
    [GhostField] public InputEvent Value;
}

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class ShootInputSystem : SystemBase
{
    private InputAction _shootAction;

    protected override void OnCreate()
    {
        _shootAction = InputSystem.actions.FindActionMap("Player").FindAction("Attack");
    }

    protected override void OnUpdate()
    {
        ShootInputComponent newShootInput = new ShootInputComponent();

        if (_shootAction.WasPressedThisFrame())
        {
            newShootInput.Value.Set();
        }

        foreach (var shootInput in SystemAPI
            .Query<RefRW<ShootInputComponent>>())
        {
            shootInput.ValueRW = newShootInput;
        }
    }
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

        foreach (var (transform, shootInput, cameraRotation, entity) in SystemAPI
            .Query<LocalTransform, ShootInputComponent, CameraRotationComponent>()
            .WithAll<Simulate>()
            .WithEntityAccess())
        {
            if (!shootInput.Value.IsSet)
            {
                continue;
            }

            float3 forwardVector = math.mul(cameraRotation.Value, new float3(0, 0, 1));

            float3 startPosition = transform.Position + new float3(0, 1.5f, 0);
            float3 endPosition = startPosition + (forwardVector * 100);

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

                    if (state.World.IsServer() && state.EntityManager.HasComponent<DamageBufferElement>(hit.Entity))
                    {
                        ecb.AppendToBuffer(hit.Entity, new DamageBufferElement { Value = 10 });
                    }

                    break;
                }
            }

            Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.red, 1);
        }
    }
}
