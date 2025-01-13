using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<PlayerData, PlayerInputData, LocalTransform>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        PlayerMovementJob job = new PlayerMovementJob
        {
            dt = SystemAPI.Time.DeltaTime
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct PlayerMovementJob : IJobEntity
{
    public float dt;
    
    public void Execute(PlayerData player, PlayerInputData input, ref LocalTransform transform)
    {
        float3 movement = new float3(input.move.x, 0, input.move.y) * player.speed * dt;
        transform.Position = transform.Translate(movement).Position;
    }
}
