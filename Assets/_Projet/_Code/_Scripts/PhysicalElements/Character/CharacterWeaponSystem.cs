using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct WaitForInstanciateWeaponsTag : IComponentData { }

[GhostComponent]
public struct ActiveWeaponTag : IComponentData { [GhostField] public bool Value; }

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct CharacterWeaponSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WaitForInstanciateWeaponsTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (prefabsRef, weaponsListRef, activeWeaponRef, chara) in SystemAPI
            .Query<RefRO<CharacterWeaponPrefab>, RefRW<CharacterWeaponsList>, RefRW<CharacterActiveWeapon>>()
            .WithAll<WaitForInstanciateWeaponsTag>()
            .WithEntityAccess())
        {
            ref readonly CharacterWeaponPrefab prefabs = ref prefabsRef.ValueRO;
            ref CharacterWeaponsList weaponsList = ref weaponsListRef.ValueRW;
            //ref CharacterActiveWeapon activeWeapon = ref activeWeaponRef.ValueRW;

            if (prefabs.MainWeaponPrefab != Entity.Null)
            {
                Entity weapon = ecb.Instantiate(prefabs.MainWeaponPrefab);
                ecb.SetComponent(chara, new CharacterActiveWeapon { Value = weapon });
                ecb.SetComponent(weapon, new WeaponOwner { Value = chara });

                //ecb.AddComponent(weapon, new ActiveWeaponTag { Value = true });

                weaponsList.List[(int)StuffType.MainWeapon] = weapon;

                int networkId = state.EntityManager.GetComponentData<GhostOwner>(chara).NetworkId;
                ecb.SetComponent(weapon, new GhostOwner() //Set owner of player to connection
                {
                    NetworkId = networkId
                });
                ecb.AppendToBuffer(chara, new LinkedEntityGroup() //Link it to connection
                {
                    Value = weapon
                });
            }
            ecb.RemoveComponent<WaitForInstanciateWeaponsTag>(chara);
        }
    }

    //ecb.SetComponent(activeWeapon.Value, new CharacterActiveWeapon { Value = ecb.Instantiate(prefabs.MainWeaponPrefab) });

    //InstanciateWeapon(ecb, prefabs.MeleeWeaponPrefab, chara, ref state, ref weaponsList, StuffType.Melee, ref activeWeapon);
    //InstanciateWeapon(ecb, prefabs.SecondWeaponPrefab, chara, ref state, ref weaponsList, StuffType.SecondaryWeapon, ref activeWeapon);
    //InstanciateWeapon(ecb, prefabs.MainWeaponPrefab, chara, ref state, ref weaponsList, StuffType.MainWeapon, ref activeWeapon);

    //void InstanciateWeapon(EntityCommandBuffer ecb, Entity prefab, Entity chara, ref SystemState state, ref CharacterWeaponsList weapons, StuffType type, ref CharacterActiveWeapon activeWeapon)
    //{
    //    if (prefab != Entity.Null)
    //    {
    //        //activeWeapon.Value = ecb.Instantiate(prefab);

    //        ecb.SetComponent(activeWeapon.Value, new CharacterActiveWeapon { Value = ecb.Instantiate(prefab) });
    //        ecb.SetComponent(activeWeapon.Value, new WeaponOwner { Value = chara });

    //        weapons.List[(int)type] = activeWeapon.Value;

    //        int networkId = state.EntityManager.GetComponentData<GhostOwner>(chara).NetworkId;

    //        ecb.SetComponent(activeWeapon.Value, new GhostOwner() //Set owner of player to connection
    //        {
    //            NetworkId = networkId
    //        });
    //        ecb.AppendToBuffer(chara, new LinkedEntityGroup() //Link it to connection
    //        {
    //            Value = activeWeapon.Value
    //        });
    //    }
    //}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//Probleme de tick
partial struct CharacterSetActiveWeapon : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<WeaponOwner>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (ownerRef, weapon) in SystemAPI
            .Query<RefRO<WeaponOwner>>()
            //.WithAbsent<ActiveWeaponTag>()
            .WithEntityAccess())
        {
            if (ownerRef.ValueRO.Value != Entity.Null)
            {
                CharacterActiveWeapon charaActiveWeapon = state.EntityManager.GetComponentData<CharacterActiveWeapon>(ownerRef.ValueRO.Value);

                if (weapon == charaActiveWeapon.Value)
                {
                    ecb.AddComponent(weapon, new ActiveWeaponTag());
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
