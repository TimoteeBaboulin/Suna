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
    [GhostField] public InputEvent Input;
    [GhostField] public LocalTransform CameraTransform;
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
            newShootInput.Input.Set();
        }

        foreach (var (shootInput, camera) in SystemAPI
            .Query<RefRW<ShootInputComponent>, RefRO<CameraAttachComponent>>())
        {
            newShootInput.CameraTransform = camera.ValueRO.transform;
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

        foreach (var (transform, shootInput, entity) in SystemAPI
            .Query<RefRO<LocalTransform>, RefRO<ShootInputComponent>>()
            .WithAll<Simulate>()
            .WithEntityAccess())
        {
            if (!shootInput.ValueRO.Input.IsSet)
            {
                continue;
            }

            float3 startPosition = shootInput.ValueRO.CameraTransform.Position;
            float3 endPosition = startPosition + (shootInput.ValueRO.CameraTransform.Forward() * 100);

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
