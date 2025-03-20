using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class InGameHUDSystem : SystemBase
{
    public class HealthArgs : EventArgs { public int Health; }
    public event EventHandler<HealthArgs> HealthChangedEvent;
    public event EventHandler HitRegister;

    [BurstCompile]
    protected override void OnCreate()
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<CurrentHealthComponent, HasServerConfirmation>();

        RequireForUpdate(GetEntityQuery(builder));
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        foreach (var (currentHealth, hasHit) in SystemAPI
            .Query<RefRO<CurrentHealthComponent>, RefRO<HasServerConfirmation>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            HealthChangedEvent?.Invoke(this, new HealthArgs { Health = currentHealth.ValueRO.Value });

            if (hasHit.ValueRO.Value)
            {
                HitRegister?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
