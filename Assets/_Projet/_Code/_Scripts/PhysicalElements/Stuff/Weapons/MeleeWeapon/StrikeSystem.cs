using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

namespace MeleeWeapon
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public partial struct StrikeSystem : ISystem
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

            var grd = SystemAPI.GetSingleton<GameResourcesDatabase>();

            //Query
            foreach (var (dynamicDataRW, databaseAccessRO, ownerRef, weapon) in SystemAPI
            .Query<RefRW<MeleeWeaponDynamicData>, RefRO<MeleeWeaponDatabaseAccess>, RefRW<StuffOwner>>()
            .WithAll<IsStuffInHand, Simulate>()
            .WithEntityAccess())
            {
                ref MeleeWeaponDynamicData dynamicData = ref dynamicDataRW.ValueRW;
                ref MeleeWeaponCommonData commonData = ref databaseAccessRO.ValueRO.GetData(ref grd);
                ref readonly Entity owner = ref ownerRef.ValueRO.Value;

                // Retrieve player input
                if (!TryGetOwnerInputRW(owner, ref state, out var inputRef)) return;
                ref CharacterInput input = ref inputRef.ValueRW;

                // Retrieve player bones
                if (!TryGetOwnerBones(owner, ref state, out var modelBonesRef)) return;
                float3 viewPos = modelBonesRef.WeaponSlotTransform.position;


                // Calculate strike rate
                if (dynamicData.strikeTimer > 0)
                    dynamicData.strikeTimer -= SystemAPI.Time.DeltaTime;

                // If the player shoots, the fire rate is valid, and there are still bullets left
                if (input.attack.IsSet && dynamicData.strikeTimer <= 0)
                {
                    dynamicData.strikeTimer += commonData.strikeRate;

                    RaycastHit hit = ClosestRayCast(input.shootRotation, viewPos, commonData.range, owner);

                    // Apply damage to the target player
                    if (hit.Entity != Entity.Null && state.World.IsServer() && state.EntityManager.HasComponent<CharacterColliderDataComponent>(hit.Entity))
                    {
                        var bodyPartData = SystemAPI.GetComponentRO<CharacterColliderDataComponent>(hit.Entity);

                        if (bodyPartData.ValueRO.CharacterEntity != owner && state.EntityManager.HasComponent<DamageBufferElement>(bodyPartData.ValueRO.CharacterEntity))
                        {
                            ecb.AppendToBuffer(bodyPartData.ValueRO.CharacterEntity, new DamageBufferElement
                            {
                                Value = commonData.damage * bodyPartData.ValueRO.DamageMultiplier
                            });
                            ecb.SetComponent(owner, new HasHitComponent { Value = true });
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

        bool TryGetOwnerBones(Entity owner, ref SystemState state, out CommonCharacterModelBonesTransform modelBones)
        {
            if (state.EntityManager.HasComponent<CommonCharacterModelBonesTransform>(owner))
            {
                modelBones = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(owner);
                return true;
            }
            else
            {
                //Debug.LogError("CharacterModelBones not found");
                modelBones = default;
                return false;
            }
        }


        RaycastHit ClosestRayCast(quaternion shootRotation, float3 viewPos, float range, Entity owner)
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
                closestHit = allHits[0];
                float closestDist = range;
                foreach (RaycastHit hit in allHits)
                {
                    // If the entity hit is the shooter, skip
                    if (hit.Entity == owner) continue;

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