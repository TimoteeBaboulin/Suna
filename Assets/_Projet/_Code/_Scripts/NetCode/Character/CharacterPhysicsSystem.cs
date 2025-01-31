using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
partial struct CharacterPhysicsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<
            CharacterControllerComponent,
            PhysicsVelocity>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CharacterApplyGravityJob job = new CharacterApplyGravityJob
        {
            DT = SystemAPI.Time.DeltaTime
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(CharacterControllerComponent), typeof(Simulate))]
public partial struct CharacterApplyGravityJob : IJobEntity
{
    [ReadOnly] public float DT;

    public void Execute(RefRW<PhysicsVelocity> physicsVelocity)
    {
        physicsVelocity.ValueRW.Linear.y += Physics.gravity.y * DT;
    }
}
