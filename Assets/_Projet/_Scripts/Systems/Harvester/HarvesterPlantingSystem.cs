using System;
using Unity.Entities;
using Unity.NetCode;

partial class HarvesterPlantingSystem : SystemBase
{
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
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.InterpolationTick;

        foreach (var (harvester, entity) in SystemAPI
            .Query<RefRO<HarvesterComponent>>()
            .WithAll<HarvesterPlanting>()
            .WithEntityAccess())
        {
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

        foreach (var (harvester, entity) in SystemAPI
            .Query<RefRO<HarvesterComponent>>()
            .WithNone<HarvesterPlanting>()
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
