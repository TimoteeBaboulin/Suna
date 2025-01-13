using System.Security.Cryptography.X509Certificates;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.InputSystem;

using RaycastHit = Unity.Physics.RaycastHit;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct ShootInput : IInputComponentData
{
    [GhostField] public InputEvent Value;
}

public partial class ShootInputSystem : SystemBase
{
    private InputAction _shootInputAction;

    protected override void OnCreate()
    {
        _shootInputAction = InputSystem.actions.FindActionMap("Player").FindAction("Attack");
    }

    protected override void OnUpdate()
    {
        ShootInput newShootInput = new ShootInput();

        if (_shootInputAction.WasPressedThisFrame())
        {
            newShootInput.Value.Set();
        }

        foreach (var shootInput in SystemAPI
            .Query<RefRW<ShootInput>>())
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
        state.RequireForUpdate<NetworkTime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        if (!networkTime.IsFirstTimeFullyPredictingTick)
        {
            return;
        }

        PhysicsWorld world = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;

        foreach (var shootInput in SystemAPI
            .Query<ShootInput>())
        {
            if (shootInput.Value.IsSet)
            {
                RaycastInput raycast = new RaycastInput
                {
                    //Start = mainCamera.transform.position,
                    //End = mainCamera.transform.forward * 1000,
                    Filter = CollisionFilter.Default
                };

                if (world.CastRay(raycast, out RaycastHit closestHit))
                {
                    //closestHit.
                }
            }
        }
    }
}
