using System;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class InGameHUDSystem : SystemBase
{
    public class HealthArgs : EventArgs { public int Health; }
    public event EventHandler<HealthArgs> HealthChangedEvent;

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<CurrentHealthComponent>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        foreach (var currentHealth in SystemAPI
            .Query<RefRO<CurrentHealthComponent>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            HealthChangedEvent?.Invoke(this, new HealthArgs { Health = currentHealth.ValueRO.Value });
        }
    }
}
