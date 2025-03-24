using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

namespace RangedWeapon
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ReloadSystem : ISystem
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
            //Eviter répétition sur le serveur du a la différence de framerate avec le client
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstPredictionTick) return;

            float dt = networkTime.ServerTickFraction * SystemAPI.Time.DeltaTime;
            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (dynamicDataRef, commonData, ownerRef, weapon) in SystemAPI
            .Query<RefRW<DynamicData>, CommonData, RefRO<StuffOwner>>()
            .WithAll<IsStuffInHand>()
            .WithEntityAccess())
            {
                //Simplification des components de l'arme
                ref DynamicData dynamicData = ref dynamicDataRef.ValueRW;
                ref readonly Entity owner = ref ownerRef.ValueRO.Value;

                //Recuperation Input joueur
                if (!TryGetOwnerInputRW(owner, ref state, out var inputRef)) return;
                ref CharacterInput input = ref inputRef.ValueRW;

                //state.EntityManager.GetSharedComponent<RangedWeaponCommonData>(weapon);

                //Calcul du reloadTimer
                if (dynamicData.reloadTimer > 0)
                    dynamicData.reloadTimer -= dt;

                if (input.reload.IsSet && dynamicData.reloadTimer <= 0 && dynamicData.currentAmmo < commonData.magazineCapacity + 1 && dynamicData.remainingAmmo != 0)
                {
                    dynamicData.reloadTimer = commonData.reloadSpeed;

                    bool bulletInChamber = dynamicData.currentAmmo > 0;

                    int ammoToAdd = Mathf.Min(commonData.magazineCapacity, dynamicData.remainingAmmo) - dynamicData.currentAmmo;
                    dynamicData.currentAmmo += ammoToAdd;
                    dynamicData.remainingAmmo -= ammoToAdd;

                    Debug.Log("Reload Finish !");

                    if (!bulletInChamber)
                    {
                        Debug.Log("Load Chamber !");
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
    }
}