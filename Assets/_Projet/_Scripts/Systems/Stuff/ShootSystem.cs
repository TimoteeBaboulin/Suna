using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Services.Multiplayer;
using Unity.Transforms;
using UnityEngine;

using RaycastHit = Unity.Physics.RaycastHit;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct HasHitComponent : IComponentData
{
    [GhostField] public bool Value;
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ShootSystem : ISystem
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
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        //Get ECB
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        float dt = SystemAPI.Time.DeltaTime;

        var grd = SystemAPI.GetSingleton<GameResourcesDatabase>();

        //Query
        foreach (var (dynamicDataRW, databaseAccessRO, ownerRef, weapon) in SystemAPI
        .Query<RefRW<RangedWeaponDynamicData>, RefRO<RangedWeaponDatabaseAccess>, RefRW<StuffDynamicData>>()
        .WithAll<IsStuffInHand, Simulate>()
        .WithEntityAccess())
        {
            ref RangedWeaponDynamicData dynamicData = ref dynamicDataRW.ValueRW;
            ref RangedWeaponCommonData commonData = ref databaseAccessRO.ValueRO.GetData(ref grd);
            ref readonly Entity owner = ref ownerRef.ValueRO.owner;

            if (owner == Entity.Null) continue;

            RefRW<CharacterViewRotation> localView = SystemAPI.GetComponentRW<CharacterViewRotation>(owner);

            //Check valid state
            if (dynamicData.state == RangedWeaponState.Idle || dynamicData.state == RangedWeaponState.Shoot)
            {
                dynamicData.state = RangedWeaponState.Idle;

                // Retrieve player input
                if (!TryGetOwnerInputRW(owner, ref state, out var inputRef)) continue;
                ref CharacterInput input = ref inputRef.ValueRW;

                // Retrieve player bones
                //if (!TryGetOwnerBones(owner, ref state, out var modelBonesRef)) return;
                //float3 viewPos = modelBonesRef.ViewBoneTransform.position;

                if (!TryGetCharacterStartShootPos(owner, ref state, out var shootStartpos)) continue;

                if (!TryGetCharacterShootRotation(owner, ref state, out var shootRotation)) return;

                // Calculate fire rate
                if (dynamicData.firerateTimer > 0)
                    dynamicData.firerateTimer -= dt;

                if (dynamicData.timeSinceLastFire < commonData.lastFireTimeMax)
                {
                    dynamicData.timeSinceLastFire += dt;
                }
                else
                {
                    dynamicData.timeSinceLastFire = commonData.lastFireTimeMax;
                    dynamicData.patternBulletIndex = 0;
                }


                // If the player shoots, the fire rate is valid, and there are still bullets left
                if (input.attack.IsSet)
                {
                    if ((commonData.isAutomatic || (!commonData.isAutomatic && !dynamicData.shotFired))
                    && dynamicData.firerateTimer <= 0 && dynamicData.currentAmmo > 0)
                    {
                        dynamicData.shotFired = true;
                        dynamicData.firerateTimer += 1.0f / (commonData.firerate / 60f); //turns RPM into RPS
                        dynamicData.state = RangedWeaponState.Shoot;
                        dynamicData.currentAmmo--;
                        dynamicData.shotFired = true;

                        float2 directionalMovement = (float2)MathUtils.Swizzle("xz", SystemAPI.GetComponent<PhysicsVelocity>(owner).Linear);
                        bool isShooterMoving = math.lengthsq(directionalMovement) > 0.1 || !SystemAPI.GetComponent<CharacterComponent>(owner).isGrounded;

                        SystemAPI.GetComponentRW<FPVVisualRecoil>(owner).ValueRW.timeSinceLastShoot = 0.0f;

                        for (int i = 0; i < commonData.roundsPerShot; i++)
                        {
                            // Apply spread on raycast
                            float2 recoil = CharacterShootUtils.TSprayPattern(dynamicData.patternBulletIndex, commonData.spread * (isShooterMoving ? 20 : 1), commonData.coefSpray, commonData.range) * dt;
                            float2 visualRecoil = CharacterShootUtils.TSprayPattern(dynamicData.patternBulletIndex, commonData.spread, commonData.coefSpray, commonData.range) * dt / 5f;
                            quaternion recoilRotation = math.normalize(quaternion.Euler(recoil.y * math.TORADIANS, recoil.x * math.TORADIANS, 0));
                            quaternion visualRecoilRotation = quaternion.Euler(visualRecoil.y * math.TORADIANS, visualRecoil.x * math.TORADIANS, 0);
                            recoilRotation = math.mul(shootRotation, recoilRotation);

                            localView.ValueRW.ShootingModifier = math.mul(localView.ValueRW.ShootingModifier, visualRecoilRotation);

                            dynamicData.patternBulletIndex++;

                            RaycastHit hit = ClosestRayCast(recoilRotation, shootStartpos, commonData.range, owner, state.EntityManager);

                            // Apply damage to the target player
                            if (state.World.IsServer())
                            {
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

                                        ecb.AppendToBuffer(CharacterBodyPartData.ValueRO.CharacterEntity, new DamageBufferElement
                                        {
                                            Value = commonData.damage * CharacterBodyPartData.ValueRO.DamageMultiplier
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
                                RangedWeaponSoundRpc soundRpc = new RangedWeaponSoundRpc()
                                {
                                    soudToPlay = RangedWeaponState.Shoot,
                                    source = weapon
                                };
                                RpcUtils.SendServerToClientRpc(ref soundRpc);
                                // === FIN SON ===

                            }
                        }

                        dynamicData.timeSinceLastFire = 0f;
                    }
                    else
                    {
                        ecb.SetComponent(owner, new HasHitComponent { Value = false });
                    }
                }
                else
                {
                    dynamicData.shotFired = false;
                }
            }

            localView.ValueRW.ShootingModifier = math.slerp(localView.ValueRW.ShootingModifier, quaternion.identity, dt);
            SystemAPI.GetComponentRW<FPVVisualRecoil>(owner).ValueRW.timeSinceLastShoot += dt;
        }

        foreach(var (dynamicDataRW, databaseAccessRO, ownerRef, grenade) in SystemAPI
            .Query<RefRW<GrenadeDynamicData>, RefRO<GrenadeDatabaseAccess>, RefRW<StuffOwner>>()
            .WithAll<IsStuffInHand, Simulate>()
            .WithDisabled<ReleasedGrenade>()
            .WithEntityAccess())
        {
            Debug.Log("JA");

            ref GrenadeDynamicData dynamicData = ref dynamicDataRW.ValueRW;
            ref GrenadeCommonData commonData = ref databaseAccessRO.ValueRO.GetData(ref grd);
            ref readonly Entity owner = ref ownerRef.ValueRO.Value;

            RefRW<CharacterViewRotation> localView = SystemAPI.GetComponentRW<CharacterViewRotation>(owner);

            // Retrieve player input
            if (!TryGetOwnerInputRW(owner, ref state, out var inputRef)) return;
            ref CharacterInput input = ref inputRef.ValueRW;

            if (!TryGetCharacterStartShootPos(owner, ref state, out var shootStartpos)) return;

            if (!TryGetCharacterShootRotation(owner, ref state, out var shootRotation)) return;

            if(dynamicData.isCooking)
            {
                dynamicData.cookingTime += dt;
            }

            if(input.attack.IsSet)
            {
                dynamicData.isCooking = true;
            }
            else
            {
                if(dynamicData.isCooking)
                {
                    if (dynamicData.cookingTime >= commonData.cookingTime)
                    {
                        UnityEngine.Debug.Log($"Grenade throw {state.World.IsClient()}");

                        SystemAPI.GetSingletonBuffer<UnequipStuffQueu>().Add(new UnequipStuffQueu
                        {
                            Owner = owner,
                            Stuff = grenade,
                            Position = shootStartpos
                        });

                        ecb.RemoveComponent<StuffOwner>(grenade);

                        ecb.SetComponentEnabled<IsStuffInHand>(grenade, false);
                        ecb.SetComponentEnabled<ReleasedGrenade>(grenade, true);

                        //ecb.AddComponent<PhysicsVelocity>(grenade);
                        //ecb.AddComponent<PhysicsMass>(grenade);
                        //ecb.AddComponent<PhysicsDamping>(grenade);
                        //ecb.AddComponent<PhysicsGravityFactor>(grenade);

                        ecb.SetComponent(grenade, new LocalTransform
                        {
                            Position = shootStartpos,
                            Rotation = shootRotation,
                            Scale = 1.0f
                        });

                        ecb.SetComponent(grenade, new PhysicsVelocity
                        {
                            Linear = math.mul(shootRotation, new float3(0f, 0f, 20f)),
                            Angular = math.mul(shootRotation, new float3(0f, 0f, 0f))
                        });

                        //ecb.SetComponent(grenade, new PhysicsMass
                        //{
                        //    InverseMass = 0.02222222f,
                        //    InverseInertia = new float3(0f, 0f, 0f),
                        //    AngularExpansionFactor = 0.5f,
                        //});

                        //ecb.SetComponent(grenade, new PhysicsDamping
                        //{
                        //    Linear = 0.1f,
                        //    Angular = 0.1f
                        //});

                        UnityEngine.Debug.Log($"Grenade thrown {state.World.IsClient()}");
                        dynamicData.isCooking = false;
                        dynamicData.cookingTime = 0.0f;
                    }
                }
            }
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

    bool TryGetCharacterRO(Entity owner, ref SystemState state, out RefRO<CharacterComponent> controller)
    {
        if (state.EntityManager.HasComponent<CharacterComponent>(owner))
        {
            controller = SystemAPI.GetComponentRO<CharacterComponent>(owner);
            return true;
        }
        else
        {
            controller = default;
            return false;
        }
    }

    bool TryGetOwnerBones(Entity owner, ref SystemState state, out ThirdPersonCharacterModelBonesTransform modelBones)
    {
        if (state.EntityManager.HasComponent<ThirdPersonCharacterModelBonesTransform>(owner))
        {
            modelBones = state.EntityManager.GetComponentData<ThirdPersonCharacterModelBonesTransform>(owner);
            return true;
        }
        else
        {
            //Debug.LogError("CharacterModelBones not found");
            modelBones = default;
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
        const int additionalRenderDelay = 1;

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
            CollidesWith = ~(1u << 6)
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