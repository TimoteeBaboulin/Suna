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
}

namespace RangedWeapon
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ShootSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StuffOwner>();

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

            float dt = networkTime.ServerTickFraction * SystemAPI.Time.DeltaTime;

            //Query
            foreach (var (dynamicDataRef, commonData, ownerRef, weapon) in SystemAPI
            .Query<RefRW<DynamicData>, CommonData, RefRW<StuffOwner>>()
            .WithAll<IsStuffInHand, Simulate>()
            .WithEntityAccess())
            {
                ref DynamicData dynamicData = ref dynamicDataRef.ValueRW;
                ref Entity owner = ref ownerRef.ValueRW.Value;

                //Check valid state
                if (!(dynamicData.state == _State.Idle || dynamicData.state == _State.Shoot)) return;
                dynamicData.state = _State.Idle;

                // Retrieve player input
                if (!TryGetOwnerInputRW(owner, ref state, out var inputRef)) return;
                ref CharacterInput input = ref inputRef.ValueRW;

                // Retrieve player bones
                if (!TryGetOwnerBones(owner, ref state, out var modelBonesRef)) return;
                float3 viewPos = modelBonesRef.ViewBoneTransform.position;

                // Calculate fire rate
                if (dynamicData.firerateTimer > 0)
                    dynamicData.firerateTimer -= dt;

                // If the player shoots, the fire rate is valid, and there are still bullets left
                if (input.attack.IsSet && dynamicData.firerateTimer <= 0 && dynamicData.currentAmmo > 0)
                {
                    dynamicData.firerateTimer += commonData.firerate;
                    dynamicData.state = _State.Shoot;
                    dynamicData.currentAmmo--;

                    // Apply spread on raycast
                    float2 recoil = CharacterShootUtils.TSprayPattern(commonData.magazineCapacity - dynamicData.currentAmmo, commonData.spread, commonData.coefSpray, commonData.range) * dt;
                    quaternion recoilRotation = math.normalize(quaternion.Euler(recoil.y * math.TORADIANS, recoil.x * math.TORADIANS, 0));
                    recoilRotation = math.mul(input.shootRotation, recoilRotation);

                    RaycastHit hit = ClosestRayCast(recoilRotation, viewPos, commonData.range, owner, state.EntityManager);

                    // Apply damage to the target player
                    if (state.World.IsServer()
                            && state.EntityManager.HasComponent<CharacterColliderDataComponent>(hit.Entity))
                    {
                        RefRO<CharacterColliderDataComponent> CharacterBodyPartData
                            = SystemAPI.GetComponentRO<CharacterColliderDataComponent>(hit.Entity);

                        if (CharacterBodyPartData.ValueRO.CharacterEntity != owner
                            && state.EntityManager.HasComponent<DamageBufferElement>(CharacterBodyPartData.ValueRO.CharacterEntity))
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
                }
                else
                {
                    ecb.SetComponent(owner, new HasHitComponent { Value = false });
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

        bool TryGetOwnerBones(Entity owner, ref SystemState state, out CharacterModelBones modelBones)
        {
            if (state.EntityManager.HasComponent<CharacterModelBones>(owner))
            {
                modelBones = state.EntityManager.GetComponentData<CharacterModelBones>(owner);
                return true;
            }
            else
            {
                //Debug.LogError("CharacterModelBones not found");
                modelBones = default;
                return false;
            }
        }


        RaycastHit ClosestRayCast(quaternion shootRotation, float3 viewPos, float range, Entity owner, in EntityManager entityManager)
        {
            LocalTransform startTransform = new LocalTransform
            {
                Position = viewPos,
                Rotation = shootRotation,
                Scale = 1,
            };

            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            float3 startPosition = startTransform.Position;
            float3 endPosition = startPosition + new float3(startTransform.Forward() * range);
            RaycastHit closestHit = default;

            CollisionFilter filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = ~(1u << 6)
            };

            RaycastInput raycastInput = new RaycastInput()
            {
                Start = startPosition,
                End = endPosition,
                Filter = filter //filtre pour partie du corps
            };

            NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
            if (physicsWorldSingleton.CastRay(raycastInput, ref allHits))
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
}