using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

//[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class GrenadeSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ReleasedGrenade>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        var grd = SystemAPI.GetSingleton<GameResourcesDatabase>();

        foreach (var (dynamicDataRW, databaseAccessRO, sddRW, grenade) in SystemAPI
            .Query<RefRW<GrenadeDynamicData>, RefRO<GrenadeDatabaseAccess>, RefRW<StuffDynamicData>>()
            .WithAll<ReleasedGrenade, Simulate>()
            .WithEntityAccess())
        {
            dynamicDataRW.ValueRW.thrownForTime += SystemAPI.Time.DeltaTime;

            ref var grenadeCommonData = ref databaseAccessRO.ValueRO.GetData(ref grd);
            ref var stuffCommonData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(grenade).GetData(ref grd);

            if (grenadeCommonData.triggerType == GrenadeTriggerType.Timer)
            {
                if (dynamicDataRW.ValueRO.thrownForTime >= grenadeCommonData.timerTriggerDelay)
                {
                    Entity thrownGrenade = Entity.Null;
                    foreach (var (inhand, thrownNade) in SystemAPI.Query<RefRO<StuffEntityInHandRef>>().WithEntityAccess())
                    {
                        if (inhand.ValueRO.Value == grenade)
                        {
                            thrownGrenade = thrownNade;
                        }
                    }

                    if (grenadeCommonData.grenadeType == GrenadeType.Frag)
                    {
                        var hits = new NativeList<DistanceHit>(Allocator.Temp);
                        float3 grenadePos = SystemAPI.GetComponent<LocalTransform>(thrownGrenade).Position;
                        float radius = grenadeCommonData.impactRadius;

                        bool hasAppliedDamage = false;

                        CollisionFilter filter = new CollisionFilter
                        {
                            BelongsTo = ~0u,
                            CollidesWith = (1u << 6)
                        };

                        if(SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(grenadePos, radius, ref hits, filter))
                        {
                            foreach (var hit in hits)
                            {
                                var entity = hit.Entity;

                                if (!SystemAPI.HasComponent<Damageable>(entity) || !SystemAPI.IsComponentEnabled<Damageable>(entity)) continue;

                                Entity damageSource = commandBuffer.CreateEntity();
                                commandBuffer.SetName(damageSource, "Damage Source");

                                Debug.Log($"Grenade hit {entity} with {grenade} at distance {hit.Distance} (source : {sddRW.ValueRO.owner})");
                                Debug.Log($"Grenade position : {grenadePos}, hit position : {hit.Position}");

                                commandBuffer.AddComponent(damageSource, new ApplyDamage
                                {
                                    source = DamageSource.Grenade,
                                    grenade = grenade,
                                    playerSource = sddRW.ValueRO.owner,
                                    targetEntity = entity,
                                    killReward = stuffCommonData.killGain,
                                    damage = math.saturate(math.lerp(1f, 0f, hit.Distance / radius)) * grenadeCommonData.inflictedDamage
                                });

                                hasAppliedDamage = true;
                            }
                        }

                        commandBuffer.DestroyEntity(thrownGrenade);
                        if (!hasAppliedDamage) commandBuffer.DestroyEntity(grenade);
                    }
                }
            }
        }

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
}