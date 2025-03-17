using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct SwitchStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
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

        foreach (var (weaponsListRef, activeWeaponRef, inputRef, chara) in SystemAPI
        .Query< RefRO<CharacterWeaponsList>, RefRW<CharacterActiveWeapon>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            //Simplification des components de l'arme
            ref readonly CharacterWeaponsList weaponsList = ref weaponsListRef.ValueRO;
            ref CharacterActiveWeapon activeWeapon = ref activeWeaponRef.ValueRW;
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            if (activeWeapon.Value == Entity.Null) continue;;

            if (input.selectNext.IsSet)
            {
                Debug.Log("Switch to Weapon up");
                ecb.SetComponent(chara, new CharacterActiveWeapon { Value = weaponsList.List.ElementAt(1) });
                ecb.AddComponent(weaponsList.List.ElementAt(0), new ActiveWeaponTag());
                ecb.RemoveComponent<ActiveWeaponTag>(weaponsList.List.ElementAt(1));
            }

            if (input.selectPrevious.IsSet)
            {
                Debug.Log("Switch to Weapon down");
                ecb.SetComponent(chara, new CharacterActiveWeapon { Value = weaponsList.List.ElementAt(0) });
                ecb.AddComponent(weaponsList.List.ElementAt(1), new ActiveWeaponTag());
                ecb.RemoveComponent<ActiveWeaponTag>(weaponsList.List.ElementAt(0));
            }
        }
    }
}