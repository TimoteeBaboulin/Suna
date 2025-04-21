using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateAfter(typeof(HitSystem))]
public partial struct DestroyTagSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (lifetime, _, entity) in SystemAPI.Query<RefRW<Lifetime>, RefRO<DestroyTag>>().WithEntityAccess())
        {
            lifetime.ValueRW.RemainingTime -= deltaTime;

            if (lifetime.ValueRO.RemainingTime <= 0f)
            {
                ecb.DestroyEntity(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
