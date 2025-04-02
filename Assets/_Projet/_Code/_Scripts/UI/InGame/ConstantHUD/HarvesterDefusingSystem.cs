using System;
using Unity.Entities;
using Unity.NetCode;

partial class HarvesterDefusingSystem : SystemBase
{
    public class HarversterDefuseRunning : EventArgs { public float time; public float maxTime; }
    public event EventHandler OnDefuseStart;
    public event EventHandler<HarversterDefuseRunning> OnDefuseRunning;
    public event EventHandler OnDefuseCancelOrEnd;
    float timeSpent = 0f;

    protected override void OnCreate()
    {
        RequireForUpdate<NetworkTime>();
    }
    protected override void OnUpdate()
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.InterpolationTick;

        foreach (var (harvester, entity) in SystemAPI
            .Query<RefRO<HarvesterComponent>>()
            .WithAll<HarvesterDefusing>()
            .WithEntityAccess())
        {
            if (timeSpent == 0f)
            {
                OnDefuseStart?.Invoke(this, EventArgs.Empty);
            }

            RefRO<HarvesterDefusing> planting = SystemAPI.GetComponentRO<HarvesterDefusing>(entity);

            timeSpent = currentTick.TicksSince(planting.ValueRO.DefuseStartedTick);
            if (timeSpent < 0f)
            {
                timeSpent = 0f;
            }

            OnDefuseRunning?.Invoke(this, new HarversterDefuseRunning() { time = timeSpent, maxTime = 60 * 4 });
        }

        foreach (var (harvester, entity) in SystemAPI
            .Query<RefRO<HarvesterComponent>>()
            .WithNone<HarvesterDefusing>()
            .WithEntityAccess())
        {
            if (timeSpent != 0f)
            {
                timeSpent = 0f;
                OnDefuseCancelOrEnd?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
