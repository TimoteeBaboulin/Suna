using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;


public struct MaxHealth : IComponentData
{
    public int Value;
}

public struct CurrentHealth : IComponentData
{
    [GhostField] public int Value;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct DamageBufferElement : IBufferElementData
{
    public int Value;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct DamageThisTick : ICommandData
{
    public NetworkTick Tick { get; set; }
    public int Value;
}

public struct HasNoHealthTag : IComponentData { }


[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
public partial struct CalculateFrameDamageSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

        foreach (var (damageBuffer, damageThisTickBuffer) in SystemAPI
            .Query<DynamicBuffer<DamageBufferElement>, DynamicBuffer<DamageThisTick>>()
            .WithAll<Simulate>())
        {
            if (damageBuffer.IsEmpty)
            {
                damageThisTickBuffer.AddCommandData(new DamageThisTick { Tick = currentTick, Value = 0 });
            }
            else
            {
                int totalDamage = 0;

                if (damageThisTickBuffer.GetDataAtTick(currentTick, out var damageThisTick))
                {
                    totalDamage = damageThisTick.Value;
                }

                foreach (var damage in damageBuffer)
                {
                    totalDamage += damage.Value;
                }

                damageThisTickBuffer.AddCommandData(new DamageThisTick { Tick = currentTick, Value = totalDamage });
                damageBuffer.Clear();
            }
        }
    }
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(CalculateFrameDamageSystem))]
public partial struct ApplyDamageSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (currentHealth, damageThisTickBuffer, entity) in SystemAPI
            .Query<RefRW<CurrentHealth>, DynamicBuffer<DamageThisTick>>()
            .WithAll<Simulate>()
            .WithEntityAccess())
        {
            if (!damageThisTickBuffer.GetDataAtTick(currentTick, out var damageThisTick))
            {
                continue;
            }

            if (damageThisTick.Tick != currentTick)
            {
                continue;
            }

            currentHealth.ValueRW.Value -= damageThisTick.Value;

            if (currentHealth.ValueRO.Value <= 0)
            {
                ecb.AddComponent<HasNoHealthTag>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
