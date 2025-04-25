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
        if (!networkTime.IsFirstPredictionTick) return;

        float dt = SystemAPI.Time.DeltaTime;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

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

                    RangedWeaponSoundRpc soundRpc = new RangedWeaponSoundRpc()
                    {
                        soudToPlay = RangedWeaponState.Reload,
                        source = weapon
                    };
                    RpcUtils.SendServerToClientRpc(ref soundRpc);
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
