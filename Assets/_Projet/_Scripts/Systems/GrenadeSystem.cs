using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

public enum GrenadeVFXType
{
    HE,
    Flashbang
}

public struct GrenadeVFXCommand : IRpcCommand
{
    public float3 position;
    public GrenadeVFXType type;
}


[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class GrenadeVFXSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<GrenadeVFXCommand>();
        RequireForUpdate<NetworkId>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GrenadeVFXCommand>>().WithEntityAccess())
        {
            if (SystemAPI.TryGetSingleton(out VisualEffetPrefabData prefabManager))
            {
                Entity explosionEffect = Entity.Null;

                if (command.ValueRO.type == GrenadeVFXType.HE)
                {
                    if (prefabManager.heGrenadeExplosion == null) { continue; }

                    explosionEffect = commandBuffer.Instantiate(prefabManager.heGrenadeExplosion);

                    if (SystemAPI.TryGetSingleton(out VFXDurationData durationData))
                    {
                        commandBuffer.AddComponent(explosionEffect, new Lifetime
                        {
                            RemainingTime = durationData.heGrenadeExplosionDuration
                        });
                    }                   
                }

                if(command.ValueRO.type == GrenadeVFXType.Flashbang)
                {
                    if (prefabManager.flashbangExplosion == null) { continue; }

                    explosionEffect = commandBuffer.Instantiate(prefabManager.flashbangExplosion);

                    if (SystemAPI.TryGetSingleton(out VFXDurationData durationData))
                    {
                        commandBuffer.AddComponent(explosionEffect, new Lifetime
                        {
                            RemainingTime = durationData.flashbangExplosionDuration
                        });
                    }
                }

                commandBuffer.AddComponent<DestroyTag>(explosionEffect);

                commandBuffer.SetComponent(explosionEffect, new LocalTransform
                {
                    Position = command.ValueRO.position,
                    Rotation = quaternion.identity,
                    Scale = 1.0f
                });
            }

            commandBuffer.DestroyEntity(entity);
        }

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateBefore(typeof(GrenadeThrowSystem))]
public partial class GrenadeSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ReleasedGrenade>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        var grd = SystemAPI.GetSingleton<GameResourcesDatabase>();

        foreach (var (dynamicDataRW, databaseAccessRO, sddRW, released, grenade) in SystemAPI
            .Query<RefRW<GrenadeDynamicData>, RefRO<GrenadeDatabaseAccess>, RefRO<StuffDynamicData>, RefRO<ReleasedGrenade>>()
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

                                commandBuffer.AddComponent(damageSource, new ApplyDamage
                                {
                                    source = DamageSource.Grenade,
                                    grenade = grenade,
                                    playerSource = released.ValueRO.thrower,
                                    targetEntity = entity,
                                    killReward = stuffCommonData.killGain,
                                    damage = math.saturate(math.lerp(1f, 0f, hit.Distance / radius)) * grenadeCommonData.inflictedDamage,
                                    sourcePosition = grenadePos
                                });
                            }
                        }

                        var command = new GrenadeVFXCommand
                        {
                            position = grenadePos,
                            type = GrenadeVFXType.HE
                        };

                        RpcUtils.SendServerToClientRpc(ref command);

                        commandBuffer.DestroyEntity(thrownGrenade);
                        commandBuffer.DestroyEntity(grenade);
                    }

                    if (grenadeCommonData.grenadeType == GrenadeType.Flashbang)
                    {
                        var hits = new NativeList<DistanceHit>(Allocator.Temp);
                        float3 grenadePos = SystemAPI.GetComponent<LocalTransform>(thrownGrenade).Position;
                        float radius = grenadeCommonData.impactRadius;
                        CollisionFilter filter = new CollisionFilter
                        {
                            BelongsTo = ~0u,
                            CollidesWith = (1u << 6)
                        };
                        if (SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(grenadePos, radius, ref hits, filter))
                        {
                            foreach (var hit in hits)
                            {
                                var entity = hit.Entity;
                                if (!SystemAPI.HasComponent<Damageable>(entity) || !SystemAPI.IsComponentEnabled<Damageable>(entity)) continue;

                                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

                                RaycastInput input = new RaycastInput()
                                {
                                    Start = grenadePos,
                                    End = hit.Position + math.up(),
                                    Filter = new CollisionFilter()
                                    {
                                        BelongsTo = ~0u,
                                        CollidesWith = 1u << 12, // Only Collide with grenade and shoot colliders
                                    }
                                };

                                bool didHit = collisionWorld.CastRay(input, out var directHit);

                                if (didHit) continue; //There's no direct line of sight with the player, so we keep going without applying effect
                                    
                                float currentEffect = SystemAPI.GetComponent<FlashGrenadeEffect>(entity).intensity;

                                if(!CharacterViewUtils.TryGetForward(entity, EntityManager, out float3 forward))
                                {
                                    UnityEngine.Debug.LogError($"Unable to get forward for {entity}");
                                }

                                float dot = math.dot(math.normalize(grenadePos - hit.Position), math.normalize(forward));

                                commandBuffer.AddComponent(entity, new FlashGrenadeEffect
                                {
                                    intensity = math.max(currentEffect, math.saturate(((grenadeCommonData.impactRadius - hit.Distance) / grenadeCommonData.impactRadius) * dot)) //Result between 0 and 1 0 = no effect, 1 = full effect
                                    // The use of max makes so that effects are not stacked or reset
                                });
                            }
                        }

                        var command = new GrenadeVFXCommand
                        {
                            position = grenadePos,
                            type = GrenadeVFXType.Flashbang
                        };

                        RpcUtils.SendServerToClientRpc(ref command);

                        commandBuffer.DestroyEntity(thrownGrenade);
                        commandBuffer.DestroyEntity(grenade);
                    }
                }
            }
        }

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial struct GrenadeThrowSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffEntityInHandRef>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        var grd = SystemAPI.GetSingleton<GameResourcesDatabase>();

        foreach (var (localTransform, physicsVelocity, stuffInHandRef, grenade) in SystemAPI
            .Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRW<StuffEntityInHandRef>>()
            .WithAll<Simulate>()
            .WithEntityAccess())
        {
            Entity stuff = stuffInHandRef.ValueRO.Value;

            if (!SystemAPI.HasComponent<ReleasedGrenade>(stuff)) continue;

            var released = SystemAPI.GetComponentRW<ReleasedGrenade>(stuff);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            float3 grenadePosition = localTransform.ValueRO.Position;

            if(!math.all(math.isfinite(grenadePosition)))
            {
                UnityEngine.Debug.LogWarning($"Grenade position is not finite: {grenadePosition}");
                continue;
            }

            RaycastInput input = new RaycastInput()
            {
                Start = grenadePosition,
                End = grenadePosition + (math.down() * 0.11f),
                Filter = new CollisionFilter()
                {
                    BelongsTo = ~0u,
                    CollidesWith = (1u << 12),
                }
            };

            RaycastHit hit = new RaycastHit();
            bool didHit = collisionWorld.CastRay(input, out hit);

            released.ValueRW.onGround = didHit;

            if(didHit)
            {
                float3 dir = physicsVelocity.ValueRO.Linear;
                float3 grenadeDir = math.normalize(new float3(dir.x, 0, dir.z));
                float3 slow = -grenadeDir * 10f;
                physicsVelocity.ValueRW.Linear += slow * SystemAPI.Time.DeltaTime;
            }

            if (!SystemAPI.TryGetSingleton<SimulationSingleton>(out var simulationSingleton))
            {
                UnityEngine.Debug.LogError("No physics found");
                return;
            }

            if (simulationSingleton.Type == SimulationType.NoPhysics)
            {
                UnityEngine.Debug.LogError("No physics type simulation");
                return;
            }

            Simulation simulation = simulationSingleton.AsSimulation();

            state.Dependency.Complete();

            int count = 0;

            foreach(var c in simulation.CollisionEvents)
            {
                count++;
            }

            UnityEngine.Debug.LogError($"{count} collisions detected this frame.");

            foreach (var collision in simulation.CollisionEvents)
            {
                Entity a = collision.EntityA;
                Entity b = collision.EntityB;

                bool aIsGrenade = state.EntityManager.HasComponent<StuffEntityInHandRef>(a);
                bool bIsGrenade = state.EntityManager.HasComponent<StuffEntityInHandRef>(b);

                if ((aIsGrenade && bIsGrenade) || (!aIsGrenade && !bIsGrenade))
                {
                    // Both are grenades or none is a grenade, do nothing
                    return;
                }

                Entity theGrenade = aIsGrenade ? a : b;

                if (state.EntityManager.HasComponent<ReleasedGrenade>(state.EntityManager.GetComponentData<StuffEntityInHandRef>(theGrenade).Value))
                    // If the entity is not a grenade, skip it
                    return;

                if (state.EntityManager.HasComponent<PhysicsVelocity>(theGrenade))
                {
                    var physicsVelocityCopy = state.EntityManager.GetComponentData<PhysicsVelocity>(theGrenade);
                    UnityEngine.Debug.Log($"Grenade velocity before: {physicsVelocityCopy.Linear}");
                    physicsVelocityCopy.Linear *= 0.6f;
                    physicsVelocityCopy.Angular *= 0.5f;

                    float angle = math.acos(math.dot(collision.Normal, math.up()) / (math.length(collision.Normal) * math.length(math.up())));
                    float damp = math.saturate(1f - angle / 90f);
                    physicsVelocityCopy.Linear = new float3(damp * physicsVelocityCopy.Linear.x, 0.6f * physicsVelocityCopy.Linear.y, damp * physicsVelocityCopy.Linear.z);

                    commandBuffer.SetComponent(theGrenade, physicsVelocityCopy);
                    UnityEngine.Debug.Log($"Grenade velocity after: {physicsVelocityCopy}");
                }
            }

            if (math.lengthsq(physicsVelocity.ValueRO.Linear) < 0.1f)
            {
                physicsVelocity.ValueRW.Linear = float3.zero;
            }
        }
    }

    public struct GrenadeCollisionJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentLookup<ReleasedGrenade> releasedGrenadeLookup;
        public ComponentLookup<PhysicsVelocity> physicsVelocityLookup;

        public void Execute(CollisionEvent eventObj)
        {
            UnityEngine.Debug.Log("Contact !");

            Entity a = eventObj.EntityA;
            Entity b = eventObj.EntityB;

            bool aIsGrenade = releasedGrenadeLookup.HasComponent(a);
            bool bIsGrenade = releasedGrenadeLookup.HasComponent(b);

            if ((aIsGrenade && bIsGrenade) || (!aIsGrenade && !bIsGrenade))
            {
                // Both are grenades or none is a grenade, do nothing
                return;
            }

            Entity grenade = aIsGrenade ? a : b;

            if (physicsVelocityLookup.HasComponent(grenade))
            {
                UnityEngine.Debug.Log($"Grenade velocity before: {physicsVelocityLookup[grenade].Linear}");
                var velocity = physicsVelocityLookup[grenade];
                velocity.Linear *= 0.6f;
                velocity.Angular *= 0.5f;

                float angle = math.acos(math.dot(eventObj.Normal, math.up()) / (math.length(eventObj.Normal) * math.length(math.up())));
                float damp = math.saturate(1f - angle / 90f);
                velocity.Linear = new float3(damp * velocity.Linear.x, 0.6f * velocity.Linear.y, damp * velocity.Linear.z);

                physicsVelocityLookup[grenade] = velocity;
                UnityEngine.Debug.Log($"Grenade velocity after: {physicsVelocityLookup[grenade].Linear}");
            }
        }
    }
}