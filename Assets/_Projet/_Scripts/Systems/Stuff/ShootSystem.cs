using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using RaycastHit = Unity.Physics.RaycastHit;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct HasHitComponent : IComponentData
{
    [GhostField] public bool Value;
    [GhostField] public bool HeadHit;
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
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
        var soundQueue = SystemAPI.GetSingletonBuffer<SoundQueue>();

        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        //Get ECB
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        float dt = SystemAPI.Time.DeltaTime;

        EntityCommandBuffer animationEcb = new EntityCommandBuffer(Allocator.Temp);

        var grd = SystemAPI.GetSingleton<GameResourcesDatabase>();

        //Query
        foreach (var (dynamicDataRW, databaseAccessRO, ownerRef, weapon) in SystemAPI
        .Query<RefRW<RangedWeaponDynamicData>, RefRO<RangedWeaponDatabaseAccess>, RefRW<StuffDynamicData>>()
        .WithAll<IsStuffInHand, Simulate>()
        .WithEntityAccess())
        {
            ref RangedWeaponDynamicData dynamicData = ref dynamicDataRW.ValueRW;
            ref RangedWeaponCommonData commonData = ref databaseAccessRO.ValueRO.GetData(ref grd);
            ref var stuffCommonData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(weapon).GetData(ref grd);
            ref readonly Entity owner = ref ownerRef.ValueRO.owner;

            if (owner == Entity.Null) continue;

            RefRW<CharacterViewRotation> localView = SystemAPI.GetComponentRW<CharacterViewRotation>(owner);
            RefRW<CharacterComponent> controller = SystemAPI.GetComponentRW<CharacterComponent>(owner);

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
                if (input.attackStarted.IsSet && !input.attackCanceled.IsSet)
                {
                    if ((commonData.isAutomatic || (!commonData.isAutomatic && !dynamicData.shotFired && !controller.ValueRO.isShooting))
                    && dynamicData.firerateTimer <= 0)
                    {
                        dynamicData.firerateTimer += 1.0f / (commonData.firerate / 60f); //turns RPM into RPS
                        dynamicData.state = RangedWeaponState.Shoot;
                        dynamicData.shotFired = true;

                        if (dynamicData.currentAmmo > 0)
                        {
                            dynamicData.currentAmmo--;

                            float2 directionalMovement = (float2)MathUtils.Swizzle("xz", SystemAPI.GetComponent<PhysicsVelocity>(owner).Linear);
                            bool isShooterMoving = math.lengthsq(directionalMovement) > 0.1 || !SystemAPI.GetComponent<CharacterComponent>(owner).isGrounded;

                            SystemAPI.GetComponentRW<FPVVisualRecoil>(owner).ValueRW.timeSinceLastShoot = 0.0f;

                            for (int i = 0; i < commonData.roundsPerShot; i++)
                            {
                                float2 recoil = default;
                                float2 visualRecoil = default;

                                if (stuffCommonData.Name.ToString() == "SKAR-18")
                                {
                                    recoil = CharacterShootUtils.SKAR18Pattern(dynamicData.patternBulletIndex, commonData.spread * (isShooterMoving ? 20 : 1), commonData.coefSpray, commonData.range) * dt;
                                    visualRecoil = CharacterShootUtils.SKAR18Pattern(dynamicData.patternBulletIndex, commonData.spread, commonData.coefSpray, commonData.range) * dt / 4f;
                                }
                                else if (stuffCommonData.Name.ToString() == "Banduka")
                                {
                                    recoil = CharacterShootUtils.TSprayPattern(dynamicData.patternBulletIndex, commonData.spread, commonData.coefSpray, commonData.range) * dt;
                                    visualRecoil = CharacterShootUtils.TSprayPattern(dynamicData.patternBulletIndex, commonData.spread, commonData.coefSpray, commonData.range) * dt;
                                }
                                else if (stuffCommonData.Name.ToString() == "Nelara")
                                {
                                    recoil = CharacterShootUtils.NelaraPattern(dynamicData.patternBulletIndex, commonData.spread * (isShooterMoving ? 20 : 1), commonData.coefSpray, commonData.range) * dt;
                                    visualRecoil = CharacterShootUtils.NelaraPattern(dynamicData.patternBulletIndex, commonData.spread, commonData.coefSpray, commonData.range) * dt / 4f;
                                }
                                else
                                {
                                    recoil = CharacterShootUtils.TSprayPattern(dynamicData.patternBulletIndex, commonData.spread * (isShooterMoving ? 20 : 1), commonData.coefSpray, commonData.range) * dt;
                                    visualRecoil = CharacterShootUtils.TSprayPattern(dynamicData.patternBulletIndex, commonData.spread, commonData.coefSpray, commonData.range) * dt / 4f;
                                }

                                quaternion recoilRotation = math.normalize(quaternion.Euler(recoil.y * math.TORADIANS, recoil.x * math.TORADIANS, 0));
                                quaternion visualRecoilRotation = quaternion.Euler(visualRecoil.y * math.TORADIANS, visualRecoil.x * math.TORADIANS, 0);
                                recoilRotation = math.mul(shootRotation, recoilRotation);

                                localView.ValueRW.ShootingModifier = math.mul(localView.ValueRW.ShootingModifier, visualRecoilRotation);

                                if (math.isnan(recoilRotation.value.x) || math.isnan(recoilRotation.value.y) || math.isnan(recoilRotation.value.z) || math.isnan(recoilRotation.value.w))
                                {
                                    Debug.LogError($"Recoil rotation is NaN {dynamicData.patternBulletIndex}");
                                    recoilRotation = quaternion.identity;
                                }

                                if (math.isnan(visualRecoilRotation.value.x) || math.isnan(visualRecoilRotation.value.y) || math.isnan(visualRecoilRotation.value.z) || math.isnan(visualRecoilRotation.value.w))
                                {
                                    Debug.LogError($"Visual recoil rotation is NaN {dynamicData.patternBulletIndex}");
                                    visualRecoilRotation = quaternion.identity;
                                }

                                dynamicData.patternBulletIndex++;

                                RaycastHit hit = ClosestRayCast(recoilRotation, shootStartpos, commonData.range, owner, state.EntityManager);

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

                                        //Head Shot Sound
                                        if (CharacterBodyPartData.ValueRO.Type == CharacterColliderType.Head && i == 0
                                            && state.World.IsServer())
                                        {
                                            SoundUtils.PlayWithRPC("Hit", "Headshot", hit.Position);
                                        }

                                        Entity damageSource = ecb.CreateEntity();

                                        ecb.AddComponent(damageSource, new ApplyDamage
                                        {
                                            source = DamageSource.Weapon,
                                            damage = commonData.damage * CharacterBodyPartData.ValueRO.DamageMultiplier,
                                            playerSource = owner,
                                            targetEntity = CharacterBodyPartData.ValueRO.CharacterEntity,
                                            killReward = stuffCommonData.killGain,
                                            weapon = Entity.Null, //TODO : Store the player weapon entity here
                                            sourcePosition = SystemAPI.GetComponentRO<LocalTransform>(owner).ValueRO.Position,
                                        });

                                        ecb.SetComponent(owner, new HasHitComponent { Value = true, HeadHit = CharacterBodyPartData.ValueRO.DamageMultiplier > 1f });
                                    }
                                }

                                // === VISUEL ===
                                HitCommand hc = new HitCommand()
                                {
                                    position = hit.Position,
                                    normal = hit.SurfaceNormal,
                                    origin = shootStartpos + SystemAPI.GetComponentRO<LocalTransform>(owner).ValueRO.Right() * 0.05f
                                };

                                if (!hc.position.Equals(float3.zero)
                                    && state.World.IsServer()) // There is such a low chance this happens in game that it's okay to not send it if this happens
                                                                      // It will prevent the client from trying to spawn a hit effect at 0,0,0 when the raycast fails to hit something
                                {
                                    RpcUtils.SendServerToClientRpc(ref hc);
                                }

                                //Muzzle Flash
                                if (state.EntityManager.HasComponent<StuffGameObjectRef>(weapon))
                                {
                                    StuffGameObjectRef goRef = state.EntityManager.GetComponentObject<StuffGameObjectRef>(weapon);
                                    WeaponVfxLink vfxLink = goRef.Value.GetComponent<WeaponVfxLink>();
                                    if (vfxLink is not null)
                                        vfxLink.Fire();
                                }
                                // === FIN VISUEL ===

                                // === SON ===
                                if (i == 0 && state.World.IsServer())
                                {
                                    SoundEmitter emitter = state.EntityManager.GetComponentData<SoundEmitter>(weapon);
                                    LocalToWorld transform = state.EntityManager.GetComponentData<LocalToWorld>(owner);

                                    SoundUtils.PlayWithRPC(ref emitter, "Shoot", transform.Position);

                                    if (!hc.position.Equals(float3.zero))
                                    {
                                        SoundUtils.PlayWithRPC("Hit", "Impact", hit.Position);
                                    }
                                }

                                // === FIN SON ===
                            }

                            dynamicData.timeSinceLastFire = 0f;
                        }
                        //NO AMMO
                        else if (state.World.IsServer())
                        {
                            SoundUtils.PlayWithRPC("Bullet", "NoBullet", shootStartpos);
                        }
                    }
                    else
                    {
                        ecb.SetComponent(owner, new HasHitComponent { Value = false });
                    }

                    controller.ValueRW.isShooting = true;
                }
                else if(!input.attackStarted.IsSet && input.attackCanceled.IsSet)
                {
                    dynamicData.shotFired = false;
                    controller.ValueRW.isShooting = false;
                }
            }

            localView.ValueRW.ShootingModifier = math.slerp(localView.ValueRW.ShootingModifier, quaternion.identity, dt * 3);
            SystemAPI.GetComponentRW<FPVVisualRecoil>(owner).ValueRW.timeSinceLastShoot += dt;
        }

        foreach (var (dynamicDataRW, databaseAccessRO, sddRW, grenade) in SystemAPI
            .Query<RefRW<GrenadeDynamicData>, RefRO<GrenadeDatabaseAccess>, RefRW<StuffDynamicData>>()
            .WithAll<IsStuffInHand, Simulate>()
            .WithDisabled<ReleasedGrenade>()
            .WithEntityAccess())
        {
            ref GrenadeDynamicData dynamicData = ref dynamicDataRW.ValueRW;
            ref GrenadeCommonData commonData = ref databaseAccessRO.ValueRO.GetData(ref grd);
            ref readonly Entity owner = ref sddRW.ValueRO.owner;

            if (owner == Entity.Null) continue;

            RefRW<CharacterViewRotation> localView = SystemAPI.GetComponentRW<CharacterViewRotation>(owner);

            // Retrieve player input
            if (!TryGetOwnerInputRW(owner, ref state, out var inputRef)) return;
            ref CharacterInput input = ref inputRef.ValueRW;

            if (!TryGetCharacterStartShootPos(owner, ref state, out var shootStartpos)) return;

            if (!TryGetCharacterShootRotation(owner, ref state, out var shootRotation)) return;

            if (dynamicData.isCooking)
            {
                dynamicData.cookingTime += dt;
            }

            if (input.attackStarted.IsSet && !input.attackCanceled.IsSet)
            {
                dynamicData.isCooking = true;

                if (state.EntityManager.HasComponent<GhostOwner>(owner))
                {
                    int networkId = SystemAPI.GetComponentRO<GhostOwner>(owner).ValueRO.NetworkId;
                    AnimationUtils.AddTriggerCommand("Throw", owner, animationEcb, networkId);
                }
            }
            else if(!input.attackStarted.IsSet && input.attackCanceled.IsSet)
            {
                if (dynamicData.isCooking)
                {
                    if (dynamicData.cookingTime >= commonData.cookingTime)
                    {
                        GameResourcesDatabase database = SystemAPI.GetSingleton<GameResourcesDatabase>();
                        DynamicBuffer<LinkedEntityGroup> linkedEntityGroup = SystemAPI.GetBuffer<LinkedEntityGroup>(owner);
                        DynamicBuffer<CharacterStuffList> characterStuffList = SystemAPI.GetBuffer<CharacterStuffList>(owner);
                        RefRW<CharacterStuffInfos> characterStuffInfos = SystemAPI.GetComponentRW<CharacterStuffInfos>(owner);
                        GhostOwner ghostOwner = SystemAPI.GetComponent<GhostOwner>(grenade);
                        ref var stuffCommonData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(grenade).GetData(ref database);

                        //StuffUtils.Unequip(linkedEntityGroup, ref characterStuffList, ref ghostOwner, ref sddRW.ValueRW, ref stuffCommonData, owner, grenade);
                        //StuffUtils.ThrowUnsafe(ref state, ref database, owner, grenade);
                        StuffUtils.Throw(ref state, linkedEntityGroup, characterStuffList, ref ghostOwner, ref sddRW.ValueRW, ref stuffCommonData, owner, grenade);

                        if (characterStuffList.ElementAt((int)StuffSlot.MainWeapon).entity != Entity.Null)
                        {
                            StuffUtils.SwitchTo(characterStuffList, characterStuffInfos, StuffSlot.MainWeapon);
                        }
                        else
                        {
                            StuffUtils.SwitchTo(characterStuffList, characterStuffInfos, StuffSlot.SecondaryWeapon);
                        }

                        //StuffUtils.Destroy(ref state, grenade); //Thrown Grenade will be instanciated separatly, so we can destroy the grenade entity

                        Entity thrownGrenade = ecb.Instantiate(sddRW.ValueRW.grenadeThrownPrefab);
                        ecb.SetName(thrownGrenade, "Thrown Grenade");

                        ecb.AddComponent(thrownGrenade, new StuffEntityInHandRef { Value = grenade });

                        ecb.SetComponentEnabled<IsStuffInHand>(grenade, false);

                        ecb.SetComponent(grenade, new ReleasedGrenade { thrower = owner });
                        ecb.SetComponentEnabled<ReleasedGrenade>(grenade, true);

                        //sddRW.ValueRW.owner = originalOwner;

                        ecb.SetComponent(thrownGrenade, new LocalTransform
                        {
                            Position = shootStartpos,
                            Rotation = shootRotation,
                            Scale = 1.0f
                        });

                        ecb.SetComponent(thrownGrenade, new PhysicsVelocity
                        {
                            Linear = math.mul(shootRotation, new float3(0f, 0f, 30f)),
                            Angular = math.mul(shootRotation, new float3(0f, 45f, 0f))
                        });

                        dynamicData.isCooking = false;
                        dynamicData.cookingTime = 0.0f;
                    }
                }
            }
        }

        animationEcb.Playback(state.EntityManager);
        animationEcb.Dispose();
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
        RaycastHit closestHit = default;

        try
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
        }
        catch (Exception)
        {
            throw;
        }

        return closestHit;
    }
}