using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct TracerRoundSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TracerRoundComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(allocator: Allocator.Temp);

        foreach (var (transformRW, tracerRoundRW, tracerRoundEntity) in 
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<TracerRoundComponent>>()
            .WithEntityAccess())
        {
            float3 direction = tracerRoundRW.ValueRO.end - tracerRoundRW.ValueRO.start;
            float3 velocity = math.normalize(direction) * (tracerRoundRW.ValueRO.speed * SystemAPI.Time.DeltaTime);
            float3 tracerPosition = transformRW.ValueRO.Position;
            float3 pathToEnd = tracerRoundRW.ValueRO.end - tracerPosition;

            if (math.lengthsq(pathToEnd) <= math.lengthsq(velocity))
            {
                ecb.DestroyEntity(tracerRoundEntity);
            }
            else
            {
                quaternion rotation = quaternion.LookRotation(pathToEnd, new float3(0, 1, 0));
                transformRW.ValueRW.Position = tracerPosition + velocity;
                transformRW.ValueRW.Rotation = rotation;
                tracerRoundRW.ValueRW.speed += SystemAPI.Time.DeltaTime * 1.5f;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
