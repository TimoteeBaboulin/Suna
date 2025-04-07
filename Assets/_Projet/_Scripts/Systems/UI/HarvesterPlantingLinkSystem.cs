using System;
using Unity.Entities;
using Unity.NetCode;

partial class HarvesterPlantingLinkSystem : SystemBase
{
    // Events and Timer
    public class HarversterPlantRunning : EventArgs { public float time; public float maxTime; }
    public event EventHandler OnPlantStart;
    public event EventHandler<HarversterPlantRunning> OnPlantRunning;
    public event EventHandler OnPlantCancelOrEnd;
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

        // On Harvester Planting Start and Running
        foreach (var (harvester, owner, entity) in SystemAPI
            .Query<RefRO<HarvesterComponent>, RefRO<StuffOwner>>()
            .WithAll<HarvesterPlanting>()
            .WithEntityAccess())
        {
            if (owner.ValueRO.Value == Entity.Null) continue;
            if (!EntityManager.IsComponentEnabled<GhostOwnerIsLocal>(owner.ValueRO.Value)) continue;
            if (timeSpent == 0f)
            {
                OnPlantStart?.Invoke(this, EventArgs.Empty);
            }

            RefRO<HarvesterPlanting> planting = SystemAPI.GetComponentRO<HarvesterPlanting>(entity);

            timeSpent = currentTick.TicksSince(planting.ValueRO.PlantStartedTick);
            if (timeSpent < 0f)
            {
                timeSpent = 0f;
            }

            OnPlantRunning?.Invoke(this, new HarversterPlantRunning() { time = timeSpent, maxTime = 60 * 4 });
        }

        // On Harvester Planting Cancel or End
        foreach (var (harvester, owner, entity) in SystemAPI
            .Query<RefRO<HarvesterComponent>, RefRO<StuffOwner>>()
            .WithDisabled<HarvesterPlanting>()
            .WithEntityAccess())
        {
            if (timeSpent != 0f)
            {
                timeSpent = 0f;
                OnPlantCancelOrEnd?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
