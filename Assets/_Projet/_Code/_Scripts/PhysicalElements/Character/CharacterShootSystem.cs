using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

using RaycastHit = Unity.Physics.RaycastHit;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct HasHitComponent : IComponentData
{
    [GhostField] public bool Value;
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct ShootSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<NetworkTime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        //Eviter répétition sur le serveur du a la différence de framerate avec le client
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.IsFirstPredictionTick) return;

        float dt = networkTime.ServerTickFraction * SystemAPI.Time.DeltaTime;
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (inputRO, weaponRO, shooter) in SystemAPI
            .Query<RefRO<CharacterInput>, RefRO<CharacterDefaultWeapon>>()
            .WithAll<Simulate>()
            .WithEntityAccess())
        {
            if (!state.EntityManager.HasComponent<RangedWeaponDynamicData>(weaponRO.ValueRO.Value)) return;

            ref readonly CharacterInput input = ref inputRO.ValueRO;
            ref readonly Entity weapon = ref weaponRO.ValueRO.Value;

            RefRW<WeaponAnimationState> animStateRW = default;
            if (state.EntityManager.HasComponent<WeaponAnimationState>(weapon))
            {
                animStateRW = SystemAPI.GetComponentRW<WeaponAnimationState>(weapon);
            }
            else
            {
                Debug.LogError("WeaponAnimationState not found");
                return;
            }
            ref WeaponAnimationState animState = ref animStateRW.ValueRW;

            RefRW<RangedWeaponDynamicData> weaponDynDataRW = default;
            if (state.EntityManager.HasComponent<RangedWeaponDynamicData>(weapon))
            {
                weaponDynDataRW = SystemAPI.GetComponentRW<RangedWeaponDynamicData>(weapon);

                if (weaponDynDataRW.ValueRW.firerateTimer > 0)
                    weaponDynDataRW.ValueRW.firerateTimer -= dt;
            }
            else
            {
                Debug.LogError("RangedWeaponDynamicData not found");
                return;
            }
            ref RangedWeaponDynamicData weaponDynData = ref weaponDynDataRW.ValueRW;

            if (input.shoot.IsSet && weaponDynData.firerateTimer <= 0 && weaponDynData.ammo > 0)
            {
                animState.IsFire = true;

                if (state.EntityManager.HasComponent<RangedWeaponDataRef>(weapon))
                {
                    RangedWeaponData weaponData = state.EntityManager.GetComponentData<RangedWeaponDataRef>(weapon).Value;

                    float3 startPosition = input.shootTransform.Position;
                    float3 endPosition = startPosition + new float3(input.shootTransform.Forward() * weaponData.range);

                    RaycastInput raycastInput = new RaycastInput()
                    {
                        Start = startPosition,
                        End = endPosition,
                        Filter = CollisionFilter.Default //filtre pour partie du corps
                    };

                    NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
                    if (physicsWorldSingleton.CastRay(raycastInput, ref allHits))
                    {
                        if (allHits.Length == 1 && allHits[0].Entity == shooter) break; //Si la seule cible rencontrée est le joeur qui a tiré, on quite

                        //Raycast récupére les hit dans le mauvais ordre, il faut les triers en fonction de la distance
                        RaycastHit closestHit = allHits[0];
                        float closestDist = weaponData.range;
                        foreach (RaycastHit hit in allHits)
                        {
                            if (hit.Entity == shooter) continue;

                            float currentDist = math.distancesq(raycastInput.Start, hit.Position);

                            if (currentDist < closestDist)
                            {
                                closestHit = hit;
                                closestDist = currentDist;
                            }
                        }

                        //Applique les degats au joueur cible
                        if (state.World.IsServer() && state.EntityManager.HasComponent<DamageBufferElement>(closestHit.Entity))
                        {
                            ecb.AppendToBuffer(closestHit.Entity, new DamageBufferElement { Value = weaponData.damage });
                            ecb.SetComponent(shooter, new HasHitComponent { Value = true });
                        }
                    }

                    weaponDynData.firerateTimer += weaponData.firerate;
                    weaponDynData.ammo--;
                    Debug.Log("Ammo " + weaponDynData.ammo);

                    Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.red, 0.5f);
                }
                else
                {
                    Debug.LogError("RangedWeaponDataRef not found");
                    return;
                }
            }
            else
            {
                animState.IsFire = false;

                ecb.SetComponent(shooter, new HasHitComponent { Value = false });
            }
        }
    }

    public bool TryGetComponentRO<T>(EntityManager entityManager, Entity entity, out IComponentData component) where T : unmanaged, IComponentData
    {
        if (entityManager.HasComponent<T>(entity))
        {
            component = entityManager.GetComponentData<T>(entity);
            return true;
        }
        else
        {
            Debug.LogError(typeof(T).Name + " not found");
        }

        component = default;
        return false;
    }
}

//[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
//public partial struct ShootSystem : ISystem
//{
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<PhysicsWorldSingleton>();
//        state.RequireForUpdate<NetworkTime>();
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
//        if (!networkTime.IsFirstPredictionTick) return;

//        float dt = networkTime.ServerTickFraction * SystemAPI.Time.DeltaTime;

//        foreach (var (inputRO, weaponRO, shooter) in SystemAPI
//            .Query<RefRO<CharacterInput>, RefRO<CharacterDefaultWeapon>>()
//            .WithAll<Simulate>()
//            .WithEntityAccess())
//        {
//            ProcessWeaponInput(ref state, inputRO, weaponRO, shooter, dt);
//        }
//    }

//    private void ProcessWeaponInput(ref SystemState state, RefRO<CharacterInput> inputRO, RefRO<CharacterDefaultWeapon> weaponRO, Entity shooter, float dt)
//    {
//        if (!state.EntityManager.HasComponent<RangedWeaponDynamicData>(weaponRO.ValueRO.Value)) return;

//        ref readonly CharacterInput input = ref inputRO.ValueRO;
//        ref readonly Entity weapon = ref weaponRO.ValueRO.Value;

//        if (!TryGetWeaponAnimationState(ref state, weapon, out var animState)) return;

//        if (!TryGetRangedWeaponDynamicData(ref state, weapon, out var weaponDynData, dt)) return;

//        if (input.shoot.IsSet && weaponDynData.firerateTimer <= 0 && weaponDynData.ammo > 0)
//        {
//            HandleShooting(ref state, input, shooter, ref animState, ref weaponDynData, weapon);
//        }
//        else
//        {
//            StopShooting(ref animState, ref state, shooter);
//        }
//    }

//    private bool TryGetWeaponAnimationState(ref SystemState state, Entity weapon, out WeaponAnimationState animState)
//    {
//        animState = default;
//        if (state.EntityManager.HasComponent<WeaponAnimationState>(weapon))
//        {
//            animState = SystemAPI.GetComponentRW<WeaponAnimationState>(weapon).ValueRW;
//            return true;
//        }

//        Debug.LogError("WeaponAnimationState not found");
//        return false;
//    }

//    private bool TryGetRangedWeaponDynamicData(ref SystemState state, Entity weapon, out RangedWeaponDynamicData weaponDynData, float dt)
//    {
//        weaponDynData = default;
//        if (state.EntityManager.HasComponent<RangedWeaponDynamicData>(weapon))
//        {
//            var weaponDynDataRW = SystemAPI.GetComponentRW<RangedWeaponDynamicData>(weapon);
//            weaponDynData = weaponDynDataRW.ValueRW;
//            weaponDynData.firerateTimer -= dt;
//            return true;
//        }

//        Debug.LogError("RangedWeaponDynamicData not found");
//        return false;
//    }

//    private void HandleShooting(ref SystemState state, CharacterInput input, Entity shooter, ref WeaponAnimationState animState, ref RangedWeaponDynamicData weaponDynData, Entity weapon)
//    {
//        animState.IsFire = true;

//        if (state.EntityManager.HasComponent<RangedWeaponDataRef>(weapon))
//        {
//            RangedWeaponData weaponData = state.EntityManager.GetComponentData<RangedWeaponDataRef>(weapon).Value;

//            PerformRaycast(ref state, input, shooter, weaponData);

//            weaponDynData.firerateTimer += weaponData.firerate;
//            weaponDynData.ammo--;

//            Debug.Log("Ammo " + weaponDynData.ammo);
//        }
//        else
//        {
//            Debug.LogError("RangedWeaponDataRef not found");
//        }
//    }

//    private void StopShooting(ref WeaponAnimationState animState, ref SystemState state, Entity shooter)
//    {
//        animState.IsFire = false;
//        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
//        ecb.SetComponent(shooter, new HasHitComponent { Value = false });
//    }

//    private void PerformRaycast(ref SystemState state, CharacterInput input, Entity shooter, RangedWeaponData weaponData)
//    {
//        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
//        float3 startPosition = input.shootTransform.Position;
//        float3 endPosition = startPosition + new float3(input.shootTransform.Forward() * weaponData.range);

//        RaycastInput raycastInput = new RaycastInput()
//        {
//            Start = startPosition,
//            End = endPosition,
//            Filter = CollisionFilter.Default //filtre pour partie du corps
//        };

//        NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
//        if (physicsWorldSingleton.CastRay(raycastInput, ref allHits))
//        {
//            if (allHits.Length == 1 && allHits[0].Entity == shooter) return; // Si la seule cible rencontrée est le joueur qui a tiré, on quitte

//            RaycastHit closestHit = FindClosestHit(allHits, raycastInput.Start, shooter);

//            ApplyDamageToTarget(ref state, closestHit, weaponData.damage, shooter);
//        }

//        Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.red, 0.5f);
//    }

//    private RaycastHit FindClosestHit(NativeList<RaycastHit> allHits, float3 startPosition, Entity shooter)
//    {
//        RaycastHit closestHit = allHits[0];
//        float closestDist = math.distancesq(startPosition, closestHit.Position);

//        foreach (RaycastHit hit in allHits)
//        {
//            if (hit.Entity == shooter) continue;

//            float currentDist = math.distancesq(startPosition, hit.Position);

//            if (currentDist < closestDist)
//            {
//                closestHit = hit;
//                closestDist = currentDist;
//            }
//        }

//        return closestHit;
//    }

//    private void ApplyDamageToTarget(ref SystemState state, RaycastHit closestHit, int damage, Entity shooter)
//    {
//        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

//        if (state.World.IsServer() && state.EntityManager.HasComponent<DamageBufferElement>(closestHit.Entity))
//        {
//            ecb.AppendToBuffer(closestHit.Entity, new DamageBufferElement { Value = damage });
//            ecb.SetComponent(shooter, new HasHitComponent { Value = true });
//        }
//    }
//}
