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
        //EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        if (!SystemAPI.TryGetSingleton<SimulationSingleton>(out var simulationSingleton))
        {
            Debug.Log("No physics found");
            return;
        }

        if (simulationSingleton.Type == SimulationType.NoPhysics)
            return;
        Simulation simulation = simulationSingleton.AsSimulation();

        Debug.Log("1");

        foreach (var triggerEvent in simulation.TriggerEvents)
        {
            if (SystemAPI.HasComponent<CharacterComponent>(triggerEvent.EntityA))
            {
                SystemAPI.GetComponentRW<CharacterComponent>(triggerEvent.EntityA).ValueRW.isOnSite = true;
            }
            else if (SystemAPI.HasComponent<CharacterComponent>(triggerEvent.EntityB))
            {
                SystemAPI.GetComponentRW<CharacterComponent>(triggerEvent.EntityB).ValueRW.isOnSite = true;
            }
        }

        Debug.Log("2");
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}
