using System;
using Unity.Entities;
using Unity.NetCode;

partial class HarvesterPlantingSystem : SystemBase
{
    public class HarversterPlantRunning : EventArgs { public float time; public float maxTime; }
    public event EventHandler OnPlantStart;
    public event EventHandler<HarversterPlantRunning> OnPlantRunning;
    public event EventHandler OnPlantCancelOrEnd;
    protected override void OnUpdate()
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.InterpolationTick;

        foreach (var harvester in SystemAPI
            .Query<RefRO<HarvesterComponent>>()
            .WithAll<HarvesterPlanting>())
        {
            float timeSpent = currentTick.TicksSince(harvester.ValueRO.PlantStartedTick);

            if (timeSpent == 0)
            {
                OnPlantStart?.Invoke(this, EventArgs.Empty);
            }

            if (timeSpent >= 69 * 4)
            {
                OnPlantCancelOrEnd?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                OnPlantRunning?.Invoke(this, new HarversterPlantRunning() { time = timeSpent, maxTime = 60 * 4 });
            }
        }
    }
}
