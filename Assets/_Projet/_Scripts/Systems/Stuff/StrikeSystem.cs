using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using RaycastHit = Unity.Physics.RaycastHit;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct StrikeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffDynamicData>();

        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<PhysicsWorldHistorySingleton>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Avoid repetition on the server due to the difference in framerate with the client
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        var soundQueue = SystemAPI.GetSingletonBuffer<SoundQueue>();

        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        //Get ECB
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        float dt = SystemAPI.Time.DeltaTime;

        var grd = SystemAPI.GetSingleton<GameResourcesDatabase>();

        //Query
        foreach (var (dynamicDataRW, databaseAccessRO, dynData, weapon) in SystemAPI
        .Query<RefRW<MeleeWeaponDynamicData>, RefRO<MeleeWeaponDatabaseAccess>, RefRW<StuffDynamicData>>()
        .WithAll<IsStuffInHand, Simulate>()
        .WithEntityAccess())
        {
            ref MeleeWeaponDynamicData dynamicData = ref dynamicDataRW.ValueRW;
            ref MeleeWeaponCommonData commonData = ref databaseAccessRO.ValueRO.GetData(ref grd);
            ref var stuffCommonData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(weapon).GetData(ref grd);
            ref readonly Entity owner = ref dynData.ValueRO.owner;

            if (owner == Entity.Null) continue;
            if (stuffCommonData.type != StuffType.MeleeWeapon) continue;

            RefRW<CharacterViewRotation> localView = SystemAPI.GetComponentRW<CharacterViewRotation>(owner);

            // Retrieve player input
            if (!TryGetOwnerInputRW(owner, ref state, out var inputRef)) continue;
            ref CharacterInput input = ref inputRef.ValueRW;

            if (!TryGetCharacterStartShootPos(owner, ref state, out var shootStartpos)) continue;

            if (!TryGetCharacterShootRotation(owner, ref state, out var shootRotation)) continue;

            // Calculate fire rate
            if (dynamicData.strikeTimer > 0)
                dynamicData.strikeTimer -= dt;

            // If the player shoots, the fire rate is valid, and there are still bullets left
            if (input.attack.IsSet)
            {
                if (dynamicData.strikeTimer <= 0)
                {
                    dynamicData.strikeTimer += 1.0f / (commonData.strikeRate / 60f); //turns RPM into RPS

                    SystemAPI.GetComponentRW<FPVVisualRecoil>(owner).ValueRW.timeSinceLastShoot = 0.0f;

                    RaycastHit hit = ClosestRayCast(shootRotation, shootStartpos, commonData.range, owner, state.EntityManager);

                    // Apply damage to the target player
                    if (state.EntityManager.HasComponent<CharacterColliderDataComponent>(hit.Entity))
                    {
                        RefRO<CharacterColliderDataComponent> CharacterBodyPartData
                        = SystemAPI.GetComponentRO<CharacterColliderDataComponent>(hit.Entity);

                        if (CharacterBodyPartData.ValueRO.CharacterEntity != owner
                            && state.EntityManager.HasComponent<DamageBufferElement>(CharacterBodyPartData.ValueRO.CharacterEntity)
                            && state.EntityManager.IsComponentEnabled<CharacterIsEnable>(CharacterBodyPartData.ValueRO.CharacterEntity))
                        {
                            SystemAPI.GetComponentRW<CurrentHealthComponent>(CharacterBodyPartData.ValueRO.CharacterEntity).ValueRW.lastDamager
                                = SystemAPI.GetComponentRO<CharacterClientAttachedComponent>(owner).ValueRO.ClientEntity; //We store Client Entity ID instead of character

                            Entity damageSource = ecb.CreateEntity();

                            ecb.AddComponent(damageSource, new ApplyDamage
                            {
                                source = DamageSource.Weapon,
                                damage = commonData.damage * CharacterBodyPartData.ValueRO.DamageMultiplier,
                                playerSource = owner,
                                targetEntity = CharacterBodyPartData.ValueRO.CharacterEntity,
                                killReward = stuffCommonData.killGain,
                                weapon = Entity.Null, //TODO : Store the player weapon entity here
                            });

                            ecb.SetComponent(owner, new HasHitComponent { Value = true });
                        }
                    }

                    // === VISUEL ===
                    HitCommand hc = new HitCommand()
                    {
                        position = hit.Position,
                        normal = hit.SurfaceNormal,
                        origin = shootStartpos + SystemAPI.GetComponentRO<LocalTransform>(owner).ValueRO.Right() * 0.05f
                    };

                    if (!hc.position.Equals(float3.zero)) // There is such a low chance this happens in game that it's okay to not send it if this happens
                                                          // It will prevent the client from trying to spawn a hit effect at 0,0,0 when the raycast fails to hit something
                    {
                        RpcUtils.SendServerToClientRpc(ref hc);
                    }
                    // === FIN VISUEL ===

                    // === SON ===
                    //SoundUtils.PlayAtEmitterWithRPC(ref state, "Shoot", weapon);
                    //SoundUtils.PlayWithRPC("Hit", "Impact", hit.Position);
                    // === FIN SON ===
                }
                else
                {
                    ecb.SetComponent(owner, new HasHitComponent { Value = false });
                }
            }

            SystemAPI.GetComponentRW<FPVVisualRecoil>(owner).ValueRW.timeSinceLastShoot += dt;
        }
    }

    bool TryGetOwnerInputRW(Entity owner, ref SystemState state, out RefRW<CharacterInput> Input)
    {
        if (state.EntityManager.HasComponent<CharacterInput>(owner))
        {
            Input = SystemAPI.GetComponentRW<CharacterInput>(owner);
            return true;
        }
        else
        {
            //Debug.LogError("CharacterInput not found");
            Input = default;
            return false;
        }
    }

    bool TryGetCharacterStartShootPos(Entity owner, ref SystemState state, out float3 shootStartpos)
    {
        if (state.EntityManager.HasComponent<CharacterShootStartPositionDelta>(owner)
            && state.EntityManager.HasComponent<LocalTransform>(owner))
        {
            shootStartpos = SystemAPI.GetComponentRO<CharacterShootStartPositionDelta>(owner).ValueRO.PositionDelta +
                SystemAPI.GetComponentRO<LocalTransform>(owner).ValueRO.Position;
            return true;
        }
        else
        {
            shootStartpos = default;
            return false;
        }
    }

    bool TryGetCharacterShootRotation(Entity owner, ref SystemState state, out quaternion shootRotation)
    {
        if (state.EntityManager.HasComponent<LocalTransform>(owner)
            && state.EntityManager.HasComponent<CharacterViewRotation>(owner))
        {
            quaternion characterRotation = SystemAPI.GetComponentRO<LocalTransform>(owner).ValueRO.Rotation;
            quaternion viewRotation = SystemAPI.GetComponentRO<CharacterViewRotation>(owner).ValueRO.ViewRotation;
            shootRotation = math.mul(characterRotation, viewRotation);
            return true;
        }
        else
        {
            shootRotation = quaternion.identity;
            return false;
        }
    }

    RaycastHit ClosestRayCast(quaternion shootRotation, float3 startPos, float range, Entity owner, in EntityManager entityManager)
    {
        const int additionalRenderDelay = 2;

        CommandDataInterpolationDelay interpolationDelay = entityManager.GetComponentData<CommandDataInterpolationDelay>(owner);
        uint delay = interpolationDelay.Delay + additionalRenderDelay;

        PhysicsWorldHistorySingleton collisionHistory = SystemAPI.GetSingleton<PhysicsWorldHistorySingleton>();
        PhysicsWorld physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick tick = networkTime.ServerTick;

        collisionHistory.GetCollisionWorldFromTick(tick, delay, ref physicsWorld, out var collWorld);

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = (1u << 12), // Collides only with 12 (Shoot and Grenade Collider (body parts are using that tag but TODO : find some other way)
        };

        float3 forward = math.mul(shootRotation, math.forward());
        RaycastInput raycastInput = new RaycastInput()
        {
            Start = startPos,
            End = startPos + new float3(forward * range),
            Filter = filter //filtre pour partie du corps
        };

        RaycastHit closestHit = default;
        NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
        if (collWorld.CastRay(raycastInput, ref allHits))
        {
            // Raycast retrieves hits in the wrong order, so they need to be sorted by distance
            float closestDist = float.MaxValue;
            foreach (RaycastHit hit in allHits)
            {
                // If the entity hit is the shooter, skip
                if (hit.Entity == owner) continue;

                if (entityManager.HasComponent<CharacterColliderDataComponent>(hit.Entity))
                {
                    Entity characterHitEntity = entityManager.GetComponentData<CharacterColliderDataComponent>(hit.Entity).CharacterEntity;
                    if (characterHitEntity == owner)
                    {
                        continue;
                    }

                    if (entityManager.HasComponent<CharacterIsEnable>(characterHitEntity)
                        && !entityManager.IsComponentEnabled<CharacterIsEnable>(characterHitEntity))
                    {
                        continue;
                    }
                }

                float currentDist = math.distancesq(raycastInput.Start, hit.Position);

                if (currentDist < closestDist)
                {
                    closestHit = hit;
                    closestDist = currentDist;
                }
            }
        }

#if !UNITY_SERVER
        Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.red, 0.5f);
#endif
        return closestHit;
    }
}