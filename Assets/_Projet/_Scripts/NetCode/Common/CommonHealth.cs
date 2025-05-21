using System;
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
    [GhostField] public float armorLevel;
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
    public int networkId;
    public float3 damageSourcePosition;
}
public struct KillDamageIndicator : IRpcCommand
{
    public ClientComponent killer;
    public ClientComponent target;
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
            foreach (var (healtRW, ghostOwnerRO, chara) in SystemAPI
            .Query<RefRW<CurrentHealthComponent>, RefRO<GhostOwner>>()
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

                            //===== SOUND VOICE LINES =====
                            TeamSideType side = PlayerHelpers.GetPlayerInTeamOnServer(ghostOwnerRO.ValueRO.NetworkId);
                            SoundUtils.PlayWithRPC(side.ToString(), "kill", pos);
                            //=============================

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
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
//[WithAll(typeof(Simulate))]
public partial struct ApplyDamageSystem : ISystem
{
    ComponentLookup<CurrentHealthComponent> currentHealthLookup;
    ComponentLookup<GhostOwner> ghostOwnerLookup;
    ComponentLookup<CharacterMoney> moneyLookup;

    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<CurrentHealthComponent, DamageThisTickCommand, Simulate>()
            .WithNone<HasNoHealthTag>();

        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<GameResourcesDatabase>();
        state.RequireForUpdate(state.GetEntityQuery(builder));

        currentHealthLookup = state.GetComponentLookup<CurrentHealthComponent>();
        ghostOwnerLookup = state.GetComponentLookup<GhostOwner>();
        moneyLookup = state.GetComponentLookup<CharacterMoney>();
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        EntityQuery query = state.GetEntityQuery(typeof(CharacterComponent));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
        NativeHashMap<Entity, TeamSideType> entityTeamTable = new NativeHashMap<Entity, TeamSideType>(entities.Length, Allocator.TempJob);

        foreach (Entity entity in entities)
        {
            var ghostOwner = state.EntityManager.GetComponentData<GhostOwner>(entity);
            var teamSideType = PlayerHelpers.GetPlayerInTeam(ghostOwner.NetworkId);
            entityTeamTable.TryAdd(entity, PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.NetworkId));
        }

        entities.Dispose();

        currentHealthLookup.Update(ref state);
        ghostOwnerLookup.Update(ref state);
        moneyLookup.Update(ref state);

        DamageSourceJob damageSourceJob = new DamageSourceJob
        {
            CurrentHealthLookup = currentHealthLookup,
            MoneyLookup = moneyLookup,
            GhostOwnerLookup = ghostOwnerLookup,
            CharacterClientAttachedComponentLookup = state.GetComponentLookup<CharacterClientAttachedComponent>(),
            ClientComponentLookup = state.GetComponentLookup<ClientComponent>(),
            entityTeamTable = entityTeamTable,
            ecb = ecb.AsParallelWriter()
        };

        state.Dependency = damageSourceJob.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        entityTeamTable.Dispose();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        ecb = new EntityCommandBuffer(Allocator.Temp);

        NativeList<DamageIndicator> damageIndicators = new NativeList<DamageIndicator>(Allocator.Temp);
        NativeList<KillDamageIndicator> killIndicators = new NativeList<KillDamageIndicator>(Allocator.Temp);

        foreach (var (command, entity) in SystemAPI.Query<RefRO<ApplyDamageCommand>>().WithEntityAccess())
        {
            Entity client = state.EntityManager.GetComponentData<CharacterClientAttachedComponent>(command.ValueRO.target).ClientEntity;
            damageIndicators.Add(new DamageIndicator
            {
                damageSourcePosition = command.ValueRO.position,
                networkId = state.EntityManager.GetComponentData<ClientComponent>(client).networkID
            });
            if (state.EntityManager.HasComponent<KillDamageCommand>(entity))
            {
                var killCommand = state.EntityManager.GetComponentData<KillDamageCommand>(entity);
                killIndicators.Add(new KillDamageIndicator
                {
                    killer = killCommand.killer,
                    target = killCommand.target
                });
            }
            ecb.DestroyEntity(entity); //Destroying the ApplyDamageCommand entity
        }

        EntityQuery queryDamage = new EntityQueryBuilder(Allocator.Temp).WithAll<ClientComponent>().Build(ref state);
        foreach (var client in queryDamage.ToEntityArray(Allocator.Temp))
        {
            for (int i = 0; i < damageIndicators.Length; i++)
            {
                DamageIndicator damageIndicator = damageIndicators[i];
                RpcUtils.SendServerToClientRpc(ref damageIndicator, client);
            }
            for (int i = 0; i < killIndicators.Length; i++)
            {
                KillDamageIndicator killIndicator = killIndicators[i];
                RpcUtils.SendServerToClientRpc(ref killIndicator, client);
            }
        }


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
    [ReadOnly] public ComponentLookup<CharacterClientAttachedComponent> CharacterClientAttachedComponentLookup;
    [ReadOnly] public ComponentLookup<ClientComponent> ClientComponentLookup;
    [ReadOnly] public NativeHashMap<Entity, TeamSideType> entityTeamTable;

    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, RefRW<ApplyDamage> damageComponent)
    {
        Entity target = damageComponent.ValueRO.targetEntity;

        if (!CurrentHealthLookup.TryGetComponent(target, out var targetHealth)) return;

        float damage = damageComponent.ValueRO.damage;
        float finalDamage = damage * (0.8f + math.lerp(0.2f, 0f, targetHealth.armorLevel / 100f));
        float delta = damage - finalDamage;

        targetHealth.Value -= math.max(finalDamage, 0);
        targetHealth.armorLevel -= math.max(delta, 0);
        ecb.SetComponent(sortKey, target, targetHealth);

        ApplyDamageCommand damageCommand = new ApplyDamageCommand
        {
            position = damageComponent.ValueRO.sourcePosition,
            target = target
        };

        ecb.AddComponent(sortKey, entity, damageCommand);

        if (targetHealth.Value <= 0)
        {
            targetHealth.Value = 0;
            ecb.SetComponent(sortKey, target, targetHealth);
            ecb.AddComponent<HasNoHealthTag>(sortKey, target);

            Entity source = damageComponent.ValueRO.playerSource;

            KillDamageCommand killDamageCommand = new();
            if (CharacterClientAttachedComponentLookup.TryGetComponent(source, out var characterClientSource))
            {
                if (ClientComponentLookup.TryGetComponent(characterClientSource.ClientEntity, out var clientSource))
                {
                    killDamageCommand.killer = clientSource;
                }
            }
            if (CharacterClientAttachedComponentLookup.TryGetComponent(target, out var characterClientTarget))
            {
                if (ClientComponentLookup.TryGetComponent(characterClientTarget.ClientEntity, out var clientTarget))
                {
                    killDamageCommand.target = clientTarget;
                }
            }
            ecb.AddComponent(sortKey, entity, killDamageCommand);

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
                        cm.money = math.clamp(cm.money, 0, cm.maxMoney);
                        ecb.SetComponent(sortKey, source, cm);
                    }
                }
            }
        }

        if (damageComponent.ValueRO.grenade != Entity.Null)
        {
            ecb.DestroyEntity(sortKey, damageComponent.ValueRO.grenade);
        }

        ecb.RemoveComponent<ApplyDamage>(sortKey, entity);
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct DamageSourcePositionSystem : ISystem
{
    public class DamageIndicatorArgs : EventArgs
    {
        public int networkId;
        public float angle;
    }
    public static EventHandler<DamageIndicatorArgs> OnDamageIndicatorReceived;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DamageIndicator>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        float3 playerPos = float3.zero;
        float3 forward = float3.zero;

        foreach (var transform in SystemAPI
            .Query<RefRW<LocalTransform>>()
            .WithAll<GhostOwnerIsLocal, CharacterComponent>())
        {
            playerPos = transform.ValueRO.Position;
            forward = transform.ValueRO.Forward();
        }

        foreach (var (damageIndicator, entity) in SystemAPI
            .Query<RefRO<DamageIndicator>>()
            .WithEntityAccess())
        {
            // Get the source position and show the damage indicator
            float3 damageDirectionFromPlayer = math.normalize(damageIndicator.ValueRO.damageSourcePosition - playerPos);
            float angle = math.degrees(math.acos(math.dot(forward, damageDirectionFromPlayer)));
            if (math.cross(forward, damageDirectionFromPlayer).y < 0)
            {
                angle = 360 - angle;
            }
            angle = math.clamp(angle, 0, 360);
            OnDamageIndicatorReceived?.Invoke(this, new DamageIndicatorArgs() { angle = angle, networkId = damageIndicator.ValueRO.networkId });

            ecb.DestroyEntity(entity); //Destroying the DamageIndicator entity
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct KillFeedRPCSystem : ISystem
{
    public class KillDamageIndicatorArgs : EventArgs
    {
        public ClientComponent killer;
        public ClientComponent target;
    }
    public static EventHandler<KillDamageIndicatorArgs> OnKillDamageIndicatorReceived;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<KillDamageIndicator>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (killDamageIndicator, entity) in SystemAPI
            .Query<RefRO<KillDamageIndicator>>()
            .WithEntityAccess())
        {
            OnKillDamageIndicatorReceived?.Invoke(this, new KillDamageIndicatorArgs() { killer = killDamageIndicator.ValueRO.killer, target = killDamageIndicator.ValueRO.target });

            ecb.DestroyEntity(entity); //Destroying the KillDamageIndicator entity
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}