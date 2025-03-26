using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using UnityEngine;

public struct MaxHealthComponent : IComponentData
{
    public float Value;
}

[GhostComponent]
public struct CurrentHealthComponent : IComponentData
{
    [GhostField] public float Value;
    [GhostField] public Entity lastDamager;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct DamageBufferElement : IBufferElementData
{
    public float Value;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct DamageThisTickCommand : ICommandData
{
    public NetworkTick Tick { get; set; }
    public float Value;
}

public struct HasNoHealthTag : IComponentData { }

[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
public partial struct CalculateFrameDamageSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<DamageBufferElement, DamageThisTickCommand, Simulate>();

        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        ComponentLookup<HasNoHealthTag> lookup = state.GetComponentLookup<HasNoHealthTag>();

        CalculateFrameDamageJob job = new CalculateFrameDamageJob
        {
            CurrentTick = currentTick,
            Lookup = lookup
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct CalculateFrameDamageJob : IJobEntity
{
    [ReadOnly] public NetworkTick CurrentTick;
    [ReadOnly] public ComponentLookup<HasNoHealthTag> Lookup;

    public void Execute(Entity entity, DynamicBuffer<DamageBufferElement> damageBuffer, DynamicBuffer<DamageThisTickCommand> damageThisTickBuffer)
    {
        if (Lookup.HasComponent(entity))
        {
            damageBuffer.Clear();
            return;
        }

        if (damageBuffer.IsEmpty)
        {
            damageThisTickBuffer.AddCommandData(new DamageThisTickCommand { Tick = CurrentTick, Value = 0 });
        }
        else
        {
            float totalDamage = 0;

            if (damageThisTickBuffer.GetDataAtTick(CurrentTick, out var damageThisTick))
            {
                totalDamage = damageThisTick.Value;
            }

            foreach (var damage in damageBuffer)
            {
                totalDamage += damage.Value;
            }

            damageThisTickBuffer.AddCommandData(new DamageThisTickCommand { Tick = CurrentTick, Value = totalDamage });
            damageBuffer.Clear();
        }
    }
}

[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(CalculateFrameDamageSystem))]
//[WithAll(typeof(Simulate))]
public partial struct ApplyDamageSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<CurrentHealthComponent, DamageThisTickCommand, Simulate>()
            .WithNone<HasNoHealthTag>();

        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    public void OnUpdate(ref SystemState state)
    {

        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        ComponentLookup<CharacterMoney> moneyLU = state.GetComponentLookup<CharacterMoney>();

        ApplyDamageJob job = new ApplyDamageJob
        {
            CurrentTick = currentTick,
            MoneyLookup = moneyLU,
            ECB = ecb.AsParallelWriter()
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);

        state.Dependency.Complete();


        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[BurstCompile]
[WithNone(typeof(HasNoHealthTag))]
public partial struct ApplyDamageJob : IJobEntity
{
    [ReadOnly] public NetworkTick CurrentTick;
    [ReadOnly] public ComponentLookup<CharacterMoney> MoneyLookup;
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute(Entity entity, [EntityIndexInQuery] int sortKey,
        RefRW<CurrentHealthComponent> currentHealth,
        DynamicBuffer<DamageThisTickCommand> damageThisTickBuffer)
    {
        if (!damageThisTickBuffer.GetDataAtTick(CurrentTick, out var damageThisTick))
        {
            return;
        }

        if (damageThisTick.Tick != CurrentTick)
        {
            return;
        }

        currentHealth.ValueRW.Value -= damageThisTick.Value;

        if (currentHealth.ValueRO.Value <= 0)
        {
            currentHealth.ValueRW.Value = 0;
            ECB.AddComponent<HasNoHealthTag>(sortKey, entity);

            //Aurelien (when the player dies, we add money to the killer)

            if (currentHealth.ValueRO.lastDamager != Entity.Null)
            {
                if (MoneyLookup.TryGetComponent(currentHealth.ValueRO.lastDamager, out var cm))
                {
                    cm.money += 300; //TMP : Link money gained with weapon used
                    ECB.SetComponent(sortKey, currentHealth.ValueRO.lastDamager, cm);
                }
            }
        }
    }
}
