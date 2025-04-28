using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;


[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct RangedWeaponReloadSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResourcesDatabase>();
        state.RequireForUpdate<StuffDynamicData>();

        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<PhysicsWorldHistorySingleton>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        //Eviter répétition sur le serveur du a la différence de framerate avec le client
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        var soundQueue = SystemAPI.GetSingletonBuffer<SoundQueue>();

        if (!networkTime.IsFirstPredictionTick) return;

        float dt = SystemAPI.Time.DeltaTime;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        EntityCommandBuffer animationEcb = new EntityCommandBuffer(Allocator.Temp);

        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();

        foreach (var (dynamicDataRef, dataAccessRef, ownerRef, weapon) in SystemAPI
        .Query<RefRW<RangedWeaponDynamicData>, RefRO<RangedWeaponDatabaseAccess>, RefRO<StuffDynamicData>>()
        .WithAll<IsStuffInHand>()
        .WithEntityAccess())
        {
            //Simplification des components de l'arme
            ref RangedWeaponDynamicData dynamicData = ref dynamicDataRef.ValueRW;
            ref readonly Entity owner = ref ownerRef.ValueRO.owner;
            ref var data = ref dataAccessRef.ValueRO.GetData(ref database);

            if (dynamicData.state != RangedWeaponState.Reload)
            {
                //Recuperation Input joueur
                if (!TryGetOwnerInputRW(owner, ref state, out var inputRef)) continue;
                ref CharacterInput input = ref inputRef.ValueRW;

                if (input.reload.IsSet && dynamicData.currentAmmo < data.magazineCapacity + 1 && dynamicData.remainingAmmo > 0)
                {
                    dynamicData.reloadTimer = data.reloadSpeed;

                    dynamicData.state = RangedWeaponState.Reload;
#if UNITY_EDITOR
                    Debug.Log("Reload Start !");
#endif
                    if (state.EntityManager.HasBuffer<CharacterStuffList>(owner)
                        && state.EntityManager.HasComponent<CharacterStuffList>(owner))
                    {
                        DynamicBuffer<CharacterStuffList> stuffList = SystemAPI.GetBuffer<CharacterStuffList>(owner);
                        RefRO<CharacterStuffInfos> stuffInfo = SystemAPI.GetComponentRO<CharacterStuffInfos>(owner);

                        Entity stuffInHand = StuffUtils.GetStuffInHand(stuffList, stuffInfo.ValueRO);

                        if (stuffInHand != Entity.Null)
                        {
                            if (state.EntityManager.HasComponent<StuffDatabaseAccess>(stuffInHand))
                            {
                                FixedString128Bytes stuffName = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffInHand).NameInDatabase;

                                if (stuffName == "LP-17"
                                    || stuffName == "FAKIR")
                                {
                                    AnimationUtils.AddTriggerCommand("ReloadHandgun", owner, animationEcb);
                                }
                                else if (stuffName == "Decimator"
                                    || stuffName == "SKAR-18")
                                {
                                    AnimationUtils.AddTriggerCommand("ReloadRifle", owner, animationEcb);
                                }
                                else if (stuffName == "Banduka")
                                {
                                    AnimationUtils.AddTriggerCommand("ReloadShotgun", owner, animationEcb);
                                }
                            }
                        }
                    }

                    SoundUtils.PlayAtEmitterWithRPC(ref state, "Reload", weapon);
                }
            }
            else
            {
                //Calcul du reloadTimer
                if (dynamicData.reloadTimer > 0)
                    dynamicData.reloadTimer -= dt;

                if (dynamicData.reloadTimer <= 0)
                {
                    bool bulletInChamber = dynamicData.currentAmmo > 0;
                    int ammoToAdd = Mathf.Min(data.magazineCapacity, dynamicData.remainingAmmo) - dynamicData.currentAmmo + (bulletInChamber ? 1 : 0);

                    dynamicData.state = RangedWeaponState.Idle;
                    dynamicData.currentAmmo += ammoToAdd;
                    dynamicData.remainingAmmo -= ammoToAdd;
#if UNITY_EDITOR
                    Debug.Log("Reload Finish !");
                    if (!bulletInChamber)
                    {
                        Debug.Log("Load Chamber !");
                    }
#endif
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
}
