using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct LookInputComponent : IInputComponentData
{
    [GhostField] public float2 Value;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct CameraRotationComponent : IComponentData
{
    [GhostField] public quaternion Value;
}

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class LookInputSystem : SystemBase
{
    private InputAction _lookAction;

    protected override void OnCreate()
    {
        _lookAction = InputSystem.actions.FindActionMap("Player").FindAction("Look");
    }

    protected override void OnUpdate()
    {
        LookInputComponent newLookInput = new LookInputComponent();

        newLookInput.Value = new float2(_lookAction.ReadValue<Vector2>());

        foreach (var lookInput in SystemAPI
            .Query<RefRW<LookInputComponent>>())
        {
            lookInput.ValueRW = newLookInput;
        }
    }
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct LookSystem : ISystem
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

        foreach (var (cameraRotation, lookInput, entity) in SystemAPI
            .Query<CameraRotationComponent, LookInputComponent>()
            .WithAll<Simulate>()
            .WithEntityAccess())
        {
            if (lookInput.Value.Equals(float2.zero))
            {
                continue;
            }

            quaternion newRotation = cameraRotation.Value;

            quaternion rotationX = quaternion.Euler(0, math.radians(lookInput.Value.x), 0);
            quaternion rotationY = quaternion.Euler(-math.radians(lookInput.Value.y), 0, 0);

            newRotation = math.mul(rotationX, newRotation);
            newRotation = math.mul(rotationY, newRotation);

            ecb.SetComponent(entity, new CameraRotationComponent { Value = newRotation });
        }
    }
}
