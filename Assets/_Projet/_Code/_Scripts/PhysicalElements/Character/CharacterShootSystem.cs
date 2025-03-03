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

        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (input, weapon, shooter) in SystemAPI
            .Query<RefRO<CharacterInput>, RefRO<CharacterDefaultWeapon>>()
            .WithAll<Simulate>()
            .WithEntityAccess())
        {
            if (state.EntityManager.HasComponent<RangedWeaponDynamicData>(weapon.ValueRO.Value))
            {
                RefRW<RangedWeaponDynamicData> weaponDynData = SystemAPI.GetComponentRW<RangedWeaponDynamicData>(weapon.ValueRO.Value);
                //weaponDynData.ValueRW.firerateTimer;
            }

            //Si tu ne tire pas, retour visuel crossair blanche
            if (input.ValueRO.shoot.IsSet)
            {
                if (state.EntityManager.HasComponent<WeaponAnimationState>(weapon.ValueRO.Value))
                {
                    RefRW<WeaponAnimationState> animState = SystemAPI.GetComponentRW<WeaponAnimationState>(weapon.ValueRO.Value);
                    animState.ValueRW.IsFire = true;
                }

                if (state.EntityManager.HasComponent<RangedWeaponDataRef>(weapon.ValueRO.Value))
                {
                    RangedWeaponData weaponData = state.EntityManager.GetComponentData<RangedWeaponDataRef>(weapon.ValueRO.Value).Value;

                    float3 startPosition = input.ValueRO.shootTransform.Position;
                    float3 endPosition = startPosition + new float3(input.ValueRO.shootTransform.Forward() * weaponData.range);

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

                    Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.red, 0.5f);
                }

            }
            else
            {
                if (state.EntityManager.HasComponent<WeaponAnimationState>(weapon.ValueRO.Value))
                {
                    RefRW<WeaponAnimationState> animState = SystemAPI.GetComponentRW<WeaponAnimationState>(weapon.ValueRO.Value);
                    animState.ValueRW.IsFire = false;
                }
                ecb.SetComponent(shooter, new HasHitComponent { Value = false });
            }
        }
    }




    //public void OnUpdate(ref SystemState state)
    //{
    //    //Eviter répétion sur le serveur du a la différence de framerate avec le client
    //    NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
    //    if (!networkTime.IsFirstPredictionTick) return;

    //    PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

    //    var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
    //    EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

    //    foreach (var (transform, input, hasHit, entity) in SystemAPI
    //        .Query<RefRO<LocalTransform>, RefRO<CharacterInput>, RefRW<HasHitComponent>>()
    //        .WithAll<Simulate>()
    //        .WithEntityAccess())
    //    {
    //        //Si tu ne tire pas, retour visuel crossair blanche
    //        if (!input.ValueRO.shoot.IsSet)
    //        {
    //            if (state.World.IsServer())
    //            {
    //                hasHit.ValueRW.Value = false;
    //            }

    //            continue;
    //        }

    //        float3 startPosition = input.ValueRO.shootTransform.Position;
    //        float3 endPosition = startPosition + new float3(input.ValueRO.shootTransform.Forward() * 100);

    //        RaycastInput raycastInput = new RaycastInput()
    //        {
    //            Start = startPosition,
    //            End = endPosition,
    //            //filtre pour partie du corps
    //            Filter = CollisionFilter.Default
    //        };

    //        //Raycast récupére les hit dans le mauvais ordre, il faut les triers en fonction de la distance
    //        NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
    //        if (physicsWorldSingleton.CastRay(raycastInput, ref allHits))
    //        {
    //            RaycastHit closestHit;
    //            //Je me suis touché ou pas ?
    //            if (allHits[0].Entity == entity
    //                && allHits.Length > 1)
    //            {
    //                closestHit = allHits[1];
    //            }
    //            else
    //            {
    //                closestHit = allHits[0];
    //            }

    //            //Trie des distances
    //            float closestDistance = math.distancesq(raycastInput.Start, closestHit.Position);

    //            foreach (var hit in allHits)
    //            {
    //                if (hit.Entity == entity)
    //                {
    //                    continue;
    //                }

    //                float distance = math.distancesq(raycastInput.Start, hit.Position);

    //                if (distance < closestDistance)
    //                {
    //                    closestHit = hit;
    //                    closestDistance = distance;
    //                }
    //            }

    //            if (state.World.IsServer()
    //                && closestHit.Entity != entity
    //                && state.EntityManager.HasComponent<DamageBufferElement>(closestHit.Entity))
    //            {
    //                ecb.AppendToBuffer(closestHit.Entity, new DamageBufferElement { Value = 10 });
    //                ecb.SetComponent(entity, new HasHitComponent { Value = true });
    //            }
    //        }

    //        if (state.World.IsServer())
    //            Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.blue, 0.5f);
    //        else
    //            Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.red, 0.5f);

    //    }
    //}
}
