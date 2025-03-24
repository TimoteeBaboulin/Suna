using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
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
            if (!networkTime.IsFirstPredictionTick) return;

            float dt = networkTime.ServerTickFraction * SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (dynamicDataRef, commonData, ownerRef, weapon) in SystemAPI
            .Query<RefRW<DynamicData>, CommonData, RefRO<StuffOwner>>()
            .WithAll<IsStuffInHand>()
            .WithEntityAccess())
            {
                ref DynamicData dynamicData = ref dynamicDataRef.ValueRW;
                ref readonly Entity owner = ref ownerRef.ValueRO.Value;

                // Retrieve player input
                if (!TryGetOwnerInputRW(owner, ref state, out var inputRef)) return;
                ref CharacterInput input = ref inputRef.ValueRW;

                // Calculate fire rate
                if (dynamicData.firerateTimer > 0)
                    dynamicData.firerateTimer -= dt;

                dynamicData.state = _State.Idle;

                // If the player shoots, the fire rate is valid, and there are still bullets left
                if (input.shoot.IsSet && dynamicData.firerateTimer <= 0 && dynamicData.currentAmmo > 0)
                {
                    dynamicData.firerateTimer += commonData.firerate;
                    dynamicData.state = _State.Shoot;
                    dynamicData.currentAmmo--;

                    RaycastHit hit = ClosestRayCast(input.shootTransform, commonData.range, owner);

                    // Apply damage to the target player
                    if (hit.Entity != Entity.Null && state.World.IsServer() && state.EntityManager.HasComponent<DamageBufferElement>(hit.Entity))
                    {
                        ecb.AppendToBuffer(hit.Entity, new DamageBufferElement { Value = commonData.damage });
                        ecb.SetComponent(owner, new HasHitComponent { Value = true });
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
                Debug.LogError("CharacterInput not found");
                Input = default;
                return false;
            }
        }


        RaycastHit ClosestRayCast(LocalTransform shootTransform, float range, Entity owner)
        {
            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            float3 startPosition = shootTransform.Position;
            float3 endPosition = startPosition + new float3(shootTransform.Forward() * range);
            RaycastHit closestHit = default;

            RaycastInput raycastInput = new RaycastInput()
            {
                Start = startPosition,
                End = endPosition,
                Filter = CollisionFilter.Default
            };

            NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
            if (physicsWorldSingleton.CastRay(raycastInput, ref allHits))
            {
                // If the only target hit is the player who fired, skip
                if (!(allHits.Length == 1 && allHits[0].Entity == owner))
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
            }
            Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.red, 0.5f);
            return closestHit;
        }
    }
}