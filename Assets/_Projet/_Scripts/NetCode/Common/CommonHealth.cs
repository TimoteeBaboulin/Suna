using System.Globalization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
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
    [GhostField] public bool killSoundAlreadyPlayed;
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
        if (state.World.IsServer())
        {
            foreach (var (healtRW, chara) in SystemAPI
            .Query<RefRW<CurrentHealthComponent>>()
            .WithEntityAccess())
            {
                if (healtRW.ValueRO.Value <= 0)
                {
                    if (!healtRW.ValueRW.killSoundAlreadyPlayed)
                    {
                        Entity killer = healtRW.ValueRO.lastDamager;
                        float3 pos = state.EntityManager.GetComponentData<LocalToWorld>(killer).Position;
                        SoundUtils.PlayWithRPC("Hit", "Kill", pos);
                        healtRW.ValueRW.killSoundAlreadyPlayed = true;
                    }
                }
                else
                {
                    healtRW.ValueRW.killSoundAlreadyPlayed = false;
                }
            }
        }

        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        ComponentLookup<HasNoHealthTag> lookup = state.GetComponentLookup<HasNoHealthTag>();

        CalculateFrameDamageJob job = new CalculateFrameDamageJob
        {
            CurrentTick = currentTick,
            Lookup = lookup
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();
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

//[BurstCompile] Pas avec les RPC des sons :(
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
        state.RequireForUpdate<GameResourcesDatabase>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        //ComponentLookup<CharacterMoney> moneyLookup = state.GetComponentLookup<CharacterMoney>();
        //ComponentLookup<ClientCharacterAttached> ccacLookup = state.GetComponentLookup<ClientCharacterAttached>();
        //BufferLookup<CharacterStuffList> stuffListLookup = state.GetBufferLookup<CharacterStuffList>();
        //ComponentLookup<IsStuffInHand> inHandLookup = state.GetComponentLookup<IsStuffInHand>();

        EntityQuery query = state.GetEntityQuery(typeof(StuffDatabaseAccess));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
        NativeHashMap<Entity, StuffCommonData> commonDataMap = new NativeHashMap<Entity, StuffCommonData>(entities.Length, Allocator.TempJob);

        GameResourcesDatabase database = SystemAPI.GetSingleton<GameResourcesDatabase>();
        foreach (var entity in entities)
        {
            ref var data = ref state.EntityManager.GetComponentData<StuffDatabaseAccess>(entity).GetData(ref database);
            commonDataMap.Add(entity, data);
        }

        entities.Dispose();

        //ApplyDamageJob job = new ApplyDamageJob
        //{
        //    CurrentTick = currentTick,
        //    MoneyLookup = moneyLookup,
        //    ClientAttachedComponents = ccacLookup,
        //    StuffListLookup = stuffListLookup,
        //    InHandLookup = inHandLookup,
        //    CommonDataMap = commonDataMap,
        //    ECB = ecb.AsParallelWriter()
        //};

        DamageSourceJob damageSourceJob = new DamageSourceJob
        {
            CurrentHealthLookup = state.GetComponentLookup<CurrentHealthComponent>(),
            MoneyLookup = state.GetComponentLookup<CharacterMoney>(),
            ecb = ecb.AsParallelWriter()
        };

        state.Dependency = damageSourceJob.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        commonDataMap.Dispose();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[WithNone(typeof(HasNoHealthTag))]
public partial struct DamageSourceJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<CurrentHealthComponent> CurrentHealthLookup;
    [ReadOnly] public ComponentLookup<CharacterMoney> MoneyLookup;

    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, RefRW<ApplyDamage> damageComponent)
    {
        Entity target = damageComponent.ValueRO.targetEntity;

        if (!CurrentHealthLookup.TryGetComponent(target, out var targetHealth)) return;

        Debug.Log($"Applying {damageComponent.ValueRO.damage} damage to {target} from {entity}");

        targetHealth.Value -= damageComponent.ValueRO.damage;
        ecb.SetComponent(sortKey, target, targetHealth);

        if (targetHealth.Value <= 0)
        {
            targetHealth.Value = 0;
            ecb.AddComponent<HasNoHealthTag>(sortKey, target);

            Entity source = damageComponent.ValueRO.playerSource;

            if (source != Entity.Null && source != target) //If the source is not null and if the source is the different than the target
            {
                if (MoneyLookup.TryGetComponent(source, out var cm))
                {
                    cm.money += damageComponent.ValueRO.killReward;
                    ecb.SetComponent(sortKey, source, cm);
                }
            }
        }

        if (damageComponent.ValueRO.grenade != Entity.Null)
        {
            ecb.DestroyEntity(sortKey, damageComponent.ValueRO.grenade);
        }

        ecb.DestroyEntity(sortKey, entity); //Destroying the DamageSource entity
    }
}

[BurstCompile]
[WithNone(typeof(HasNoHealthTag))]
public partial struct ApplyDamageJob : IJobEntity
{
    [ReadOnly] public NetworkTick CurrentTick;
    [ReadOnly] public ComponentLookup<CharacterMoney> MoneyLookup;
    [ReadOnly] public ComponentLookup<ClientCharacterAttached> ClientAttachedComponents;
    [ReadOnly] public BufferLookup<CharacterStuffList> StuffListLookup;
    [ReadOnly] public ComponentLookup<IsStuffInHand> InHandLookup;
    [ReadOnly] public NativeHashMap<Entity, StuffCommonData> CommonDataMap;
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
                Entity client = currentHealth.ValueRO.lastDamager;

                if (MoneyLookup.TryGetComponent(client, out var cm) && ClientAttachedComponents.TryGetComponent(client, out var chara))
                {
                    if (StuffListLookup.TryGetBuffer(chara.Value, out var stuffList))
                    {
                        foreach (var element in stuffList)
                        {
                            if (element.entity == Entity.Null) continue;

                            if (InHandLookup.TryGetComponent(element.entity, out var inHand) && InHandLookup.IsComponentEnabled(element.entity))
                            {
                                cm.money += CommonDataMap[element.entity].killGain;
                                ECB.SetComponent(sortKey, client, cm);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
