using System.Globalization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
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

public struct DamageIndicator : IRpcCommand
{
    public float3 damageSourcePosition;
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
                        if (state.EntityManager.Exists(killer))
                        {
                            float3 pos = state.EntityManager.GetComponentData<LocalToWorld>(killer).Position;
                            SoundUtils.PlayWithRPC("Hit", "Kill", pos);
                            healtRW.ValueRW.killSoundAlreadyPlayed = true;
                        }
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

        EntityQuery query = state.GetEntityQuery(typeof(CharacterComponent));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
        NativeHashMap<Entity, TeamSideType> entityTeamTable = new NativeHashMap<Entity, TeamSideType>(entities.Length, Allocator.TempJob);

        foreach(Entity entity in entities)
        {
            var ghostOwner = state.EntityManager.GetComponentData<GhostOwner>(entity);
            var teamSideType = PlayerHelpers.GetPlayerInTeam(ghostOwner.NetworkId);
            entityTeamTable.TryAdd(entity, PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.NetworkId));
        }

        entities.Dispose();

        DamageSourceJob damageSourceJob = new DamageSourceJob
        {
            CurrentHealthLookup = state.GetComponentLookup<CurrentHealthComponent>(),
            MoneyLookup = state.GetComponentLookup<CharacterMoney>(),
            GhostOwnerLookup = state.GetComponentLookup<GhostOwner>(),
            entityTeamTable = entityTeamTable,
            ecb = ecb.AsParallelWriter()
        };

        state.Dependency = damageSourceJob.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        entityTeamTable.Dispose();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[WithNone(typeof(HasNoHealthTag))]
public partial struct DamageSourceJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<CurrentHealthComponent> CurrentHealthLookup;
    [ReadOnly] public ComponentLookup<CharacterMoney> MoneyLookup;
    [ReadOnly] public ComponentLookup<GhostOwner> GhostOwnerLookup;
    [ReadOnly] public NativeHashMap<Entity, TeamSideType> entityTeamTable;

    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, RefRW<ApplyDamage> damageComponent)
    {
        Entity target = damageComponent.ValueRO.targetEntity;

        if (!CurrentHealthLookup.TryGetComponent(target, out var targetHealth)) return;

        targetHealth.Value -= damageComponent.ValueRO.damage;
        ecb.SetComponent(sortKey, target, targetHealth);

        ApplyDamageCommand damageCommand = new ApplyDamageCommand
        {
            position = damageComponent.ValueRO.sourcePosition
        };

        RpcUtils.SendServerToClientRpc(ref damageCommand, target);

        //DamageIndicator damageIndicator = new DamageIndicator
        //{
        //    damageSourcePosition = damageComponent.ValueRO.sourcePosition
        //};

        //RpcUtils.SendServerToClientRpc(ref damageIndicator, target);

        if (targetHealth.Value <= 0)
        {
            targetHealth.Value = 0;
            ecb.AddComponent<HasNoHealthTag>(sortKey, target);

            Entity source = damageComponent.ValueRO.playerSource;

            if (source != Entity.Null && source != target) //If the source is not null and if the source is the different than the target
            {
                if (GhostOwnerLookup.TryGetComponent(source, out var ghostOwnerSource) &&
                   GhostOwnerLookup.TryGetComponent(target, out var ghostOwnerTarget) &&
                   entityTeamTable.TryGetValue(source, out var sourceTeam) &&
                   entityTeamTable.TryGetValue(target, out var targetTeam) &&
                   sourceTeam != targetTeam)
                // Make sure money is given only when killing a player and when it's not a team kill
                {
                    if (MoneyLookup.TryGetComponent(source, out var cm))
                    {
                        cm.money += damageComponent.ValueRO.killReward;
                        ecb.SetComponent(sortKey, source, cm);
                    }
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
public partial struct DamageSourcePositionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
        state.RequireForUpdate<ApplyDamageCommand>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach(var (damageCommand, entity) in SystemAPI
            .Query<RefRW<ApplyDamageCommand>>()
            .WithEntityAccess())
        {
            // Get the source position and show the damage indicator

            ecb.DestroyEntity(entity); //Destroying the ApplyDamageCommand entity
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}