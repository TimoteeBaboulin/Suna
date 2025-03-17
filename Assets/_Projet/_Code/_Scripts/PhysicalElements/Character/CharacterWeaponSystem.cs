using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct WaitForInstanciateWeaponsTag : IComponentData { }

[GhostComponent]
public struct StuffInHandTag : IComponentData { }

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
            .Query<RefRO<CharacterWeaponPrefab>, RefRW<CharacterStuffList>, RefRW<CharacterStuffInHandType>>()
            .WithAll<WaitForInstanciateWeaponsTag>()
            .WithEntityAccess())
        {
            ref readonly CharacterWeaponPrefab prefabs = ref prefabsRef.ValueRO;
            ref CharacterStuffList weaponsList = ref weaponsListRef.ValueRW;
            //ref CharacterActiveWeapon activeWeapon = ref activeWeaponRef.ValueRW;

            InstanciateWeapon(ecb, prefabs.MeleeWeaponPrefab, chara, ref state, ref weaponsList, StuffType.Melee);
            InstanciateWeapon(ecb, prefabs.SecondWeaponPrefab, chara, ref state, ref weaponsList, StuffType.SecondaryWeapon);
            InstanciateWeapon(ecb, prefabs.MainWeaponPrefab, chara, ref state, ref weaponsList, StuffType.MainWeapon);

            //if (prefabs.MainWeaponPrefab != Entity.Null)
            //{
            //    Entity weapon = ecb.Instantiate(prefabs.MainWeaponPrefab);
            //    ecb.SetComponent(chara, new CharacterActiveWeapon { Value = weapon });
            //    ecb.SetComponent(weapon, new WeaponOwner { Value = chara });

            //    //ecb.AddComponent(weapon, new ActiveWeaponTag { Value = true });

            //    weaponsList.List[(int)StuffType.MainWeapon] = weapon;

            //    int networkId = state.EntityManager.GetComponentData<GhostOwner>(chara).NetworkId;
            //    ecb.SetComponent(weapon, new GhostOwner() //Set owner of player to connection
            //    {
            //        NetworkId = networkId
            //    });
            //    ecb.AppendToBuffer(chara, new LinkedEntityGroup() //Link it to connection
            //    {
            //        Value = weapon
            //    });
            //}
            ecb.RemoveComponent<WaitForInstanciateWeaponsTag>(chara);
        }
    }

    void InstanciateWeapon(EntityCommandBuffer ecb, Entity prefab, Entity chara, ref SystemState state, ref CharacterStuffList weaponsList, StuffType type)
    {
        if (prefab != Entity.Null)
        {
            Entity weapon = ecb.Instantiate(prefab);
            ecb.SetComponent(chara, new CharacterStuffInHandType { Value = type });
            ecb.SetComponent(weapon, new WeaponOwner { Value = chara });

            weaponsList.List[(int)type] = weapon;

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
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//Probleme de tick
partial struct CharacterSetActiveStuff : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (ownerRef, rangedWeaponDataRef, weapon) in SystemAPI
            .Query<RefRO<WeaponOwner>, RangedWeaponDataRef>()
            .WithAbsent<StuffInHandTag>()
            .WithEntityAccess())
        {
            if (ownerRef.ValueRO.Value != Entity.Null)
            {
                CharacterStuffInHandType stuffInHandType = state.EntityManager.GetComponentData<CharacterStuffInHandType>(ownerRef.ValueRO.Value);

                if (rangedWeaponDataRef.Value.type == stuffInHandType.Value)
                {
                    ecb.AddComponent(weapon, new StuffInHandTag());
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
