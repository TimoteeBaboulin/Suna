using System;
using Unity.Entities;
using Unity.NetCode;

partial class HarvesterDefusingLinkSystem : SystemBase
{
    // Events and Timer
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
        // Get Current Tick
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.InterpolationTick;

        // On Harvester Defusing Start and Running
        foreach (var (harvester, entity) in SystemAPI
            .Query<RefRO<HarvesterComponent>>()
            .WithAll<HarvesterDefusing>()
            .WithEntityAccess())
        {
            RefRO<HarvesterDefusing> defusing = SystemAPI.GetComponentRO<HarvesterDefusing>(entity);
            if (defusing.ValueRO.Defuser == Entity.Null) continue;
            if (!EntityManager.IsComponentEnabled<GhostOwnerIsLocal>(defusing.ValueRO.Defuser)) continue;
            if (timeSpent == 0f)
            {
                OnDefuseStart?.Invoke(this, EventArgs.Empty);
            }

            timeSpent = currentTick.TicksSince(defusing.ValueRO.DefuseStartedTick);
            if (timeSpent < 0f)
            {
                timeSpent = 0f;
            }

            OnDefuseRunning?.Invoke(this, new HarversterDefuseRunning() { time = timeSpent, maxTime = 60 * 4 });
        }

        // On Harvester Defusing Cancel or End
        foreach (var (harvester, entity) in SystemAPI
            .Query<RefRO<HarvesterComponent>>()
            .WithDisabled<HarvesterDefusing>()
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
