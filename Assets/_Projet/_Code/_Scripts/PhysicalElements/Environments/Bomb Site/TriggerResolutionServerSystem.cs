using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSimulationGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct TriggerResolutionServerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<SimulationSingleton>(out var simulationSingleton))
        {
            Debug.Log("No physics found");
            return;
        }

        if (simulationSingleton.Type == SimulationType.NoPhysics)
            return;
        Simulation simulation = simulationSingleton.AsSimulation();

        state.Dependency.Complete();

        foreach (var triggerEvent in simulation.TriggerEvents)
        {
            if (SystemAPI.HasComponent<CharacterComponent>(triggerEvent.EntityA)
                && SystemAPI.HasComponent<BombSiteTriggerComponent>(triggerEvent.EntityB))
            {
                SystemAPI.GetComponentRW<CharacterComponent>(triggerEvent.EntityA).ValueRW.isOnSite = true;
            }
            else if (SystemAPI.HasComponent<CharacterComponent>(triggerEvent.EntityB)
                && SystemAPI.HasComponent<BombSiteTriggerComponent>(triggerEvent.EntityA))
            {
                SystemAPI.GetComponentRW<CharacterComponent>(triggerEvent.EntityB).ValueRW.isOnSite = true;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}
