using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct GrenadeEffectsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FlashGrenadeEffect>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (flashEffect, entity) in SystemAPI.Query<RefRW<FlashGrenadeEffect>>().WithEntityAccess())
        {
            flashEffect.ValueRW.intensity -= SystemAPI.Time.DeltaTime / 4f;
            flashEffect.ValueRW.intensity = math.saturate(flashEffect.ValueRW.intensity);
        }
    }

    public void OnDestroy(ref SystemState state)
    {

    }
}