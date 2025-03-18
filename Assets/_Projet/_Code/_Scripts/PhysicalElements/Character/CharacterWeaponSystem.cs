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

        foreach (var (prefabsRef, stuffListRef, stuffInHandTypeRef, chara) in SystemAPI
            .Query<RefRO<CharacterStuffPrefab>, RefRW<CharacterStuffList>, RefRW<CharacterStuffInHandType>>()
            .WithAll<WaitForInstanciateWeaponsTag>()
            .WithEntityAccess())
        {
            ref readonly CharacterStuffPrefab prefabs = ref prefabsRef.ValueRO;
            ref CharacterStuffList weaponsList = ref stuffListRef.ValueRW;

            //InstanciateStuff(ecb, prefabs.MeleeWeaponPrefab, chara, ref state, ref weaponsList, StuffType.Melee);
            //InstanciateStuff(ecb, prefabs.SecondWeaponPrefab, chara, ref state, ref weaponsList, StuffType.SecondaryWeapon);
            InstanciateStuff(ecb, prefabs.MainWeaponPrefab, chara, ref state, ref weaponsList, StuffType.MainWeapon);

            ecb.RemoveComponent<WaitForInstanciateWeaponsTag>(chara);
        }
    }

    void InstanciateStuff(EntityCommandBuffer ecb, Entity prefab, Entity chara, ref SystemState state, ref CharacterStuffList stuffList, StuffType type)
    {
        if (prefab != Entity.Null)
        {
            Entity weapon = ecb.Instantiate(prefab);
            ecb.SetComponent(chara, new CharacterStuffInHandType { Value = type });
            ecb.SetComponent(weapon, new StuffOwner { Value = chara });

            ecb.AddComponent(weapon, new PendingStuffTag());

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

public struct PendingStuffTag : IComponentData { }
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct ProcessPendingStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PendingStuffTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (owner, stuffInfos, stuff) in SystemAPI
            .Query<RefRO<StuffOwner>, StuffInfos>()
            .WithAll<PendingStuffTag>()
            .WithEntityAccess())
        {
            var weaponsList = SystemAPI.GetComponentRW<CharacterStuffList>(owner.ValueRO.Value);
            weaponsList.ValueRW.List[(int)stuffInfos.type] = stuff;

            StuffType stuffInHandType = SystemAPI.GetComponent<CharacterStuffInHandType>(owner.ValueRO.Value).Value;
            ecb.AddComponent(weaponsList.ValueRW.List[(int)stuffInHandType], new StuffInHandTag()); //TODO : Duplicate

            ecb.RemoveComponent<PendingStuffTag>(stuff);
        }
    }
}

//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
////TODO : Probleme de tick @Leonnel
//partial struct CharacterSetActiveStuff : ISystem
//{
//    public void OnUpdate(ref SystemState state)
//    {
//        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

//        foreach (var (ownerRef, rangedWeaponDataRef, weapon) in SystemAPI
//            .Query<RefRO<WeaponOwner>, RangedWeaponDataRef>()
//            .WithAbsent<StuffInHandTag>()
//            .WithEntityAccess())
//        {
//            if (ownerRef.ValueRO.Value != Entity.Null)
//            {
//                CharacterStuffInHandType stuffInHandType = state.EntityManager.GetComponentData<CharacterStuffInHandType>(ownerRef.ValueRO.Value);

//                if (rangedWeaponDataRef.Value.type == stuffInHandType.Value)
//                {
//                    ecb.AddComponent(weapon, new StuffInHandTag());
//                }
//            }
//        }

//        ecb.Playback(state.EntityManager);
//        ecb.Dispose();
//    }
//}
