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
        state.RequireForUpdate<WeaponOwner>();
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

        foreach (var (dynamicDataRef, commonDataRef, ownerRef, animStateRef, weapon) in SystemAPI
        .Query<RefRW<RangedWeaponDynamicData>, RangedWeaponDataRef, RefRO<WeaponOwner>, RefRW<WeaponAnimationState>>()
        .WithAll<Simulate>()
        .WithEntityAccess())
        {
            //Simplification des components de l'arme
            ref RangedWeaponDynamicData dynamicData = ref dynamicDataRef.ValueRW;
            ref WeaponAnimationState animState = ref animStateRef.ValueRW;
            ref readonly Entity owner = ref ownerRef.ValueRO.Value;
            RangedWeaponData commonData = commonDataRef.Value;

            //Recuperation Input joueur
            if (!TryGetOwnerInputRW(owner, ref state, out var inputRef)) return;
            ref CharacterInput input = ref inputRef.ValueRW;

            //Calcul du firerate
            if (dynamicData.firerateTimer > 0)
                dynamicData.firerateTimer -= dt;

            animState.IsFire = false;

            //Si le joueur tire, que la cadence de tir est valide et qu'il y a encore des balles
            if (input.shoot.IsSet && dynamicData.firerateTimer <= 0 && dynamicData.currentAmmo > 0)
            {
                dynamicData.firerateTimer = commonData.firerate;
                dynamicData.currentAmmo--;
                animState.IsFire = true;

                float3 startPosition = input.shootTransform.Position;
                float3 endPosition = startPosition + new float3(input.shootTransform.Forward() * commonData.range);
                bool isHitSomething = false;

                RaycastInput raycastInput = new RaycastInput()
                {
                    Start = startPosition,
                    End = endPosition,
                    Filter = CollisionFilter.Default
                };

                NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
                if (physicsWorldSingleton.CastRay(raycastInput, ref allHits))
                {
                    //Si la seule cible rencontrée est le joeur qui a tiré, on skip
                    if (!(allHits.Length == 1 && allHits[0].Entity == owner))
                    {
                        //Raycast récupére les hits dans le mauvais ordre, il faut les triers en fonction de la distance
                        RaycastHit closestHit = allHits[0];
                        float closestDist = commonData.range;
                        foreach (RaycastHit hit in allHits)
                        {
                            //si l'entité rencontré est le tireur, on skip
                            if (hit.Entity == owner) continue;

                            float currentDist = math.distancesq(raycastInput.Start, hit.Position);

                            if (currentDist < closestDist)
                            {
                                closestHit = hit;
                                closestDist = currentDist;
                                isHitSomething = true;
                            }
                        }

                        //Applique les degats au joueur cible
                        if (isHitSomething && state.World.IsServer() && state.EntityManager.HasComponent<DamageBufferElement>(closestHit.Entity))
                        {
                            ecb.AppendToBuffer(closestHit.Entity, new DamageBufferElement { Value = commonData.damage });
                            ecb.SetComponent(owner, new HasHitComponent { Value = true });
                        }
                    }
                }

                Debug.Log("Ammo " + dynamicData.currentAmmo);
                Debug.DrawRay(raycastInput.Start, raycastInput.End - raycastInput.Start, Color.red, 0.5f);
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
}