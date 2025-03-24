using Unity.Entities;
using Unity.NetCode;

using Unity.Transforms;
using UnityEngine;
using UnityEngine.TextCore.Text;


public struct WaitForInstanciateStuffTag : IComponentData { }


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct InstanciateStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WaitForInstanciateStuffTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (prefabsRef, stuffListRef, stuffInHandTypeRef, chara) in SystemAPI
            .Query<RefRO<CharacterStuffPrefab>, RefRW<CharacterStuffList>, RefRW<CharacterStuffInHandType>>()
            .WithAll<WaitForInstanciateStuffTag>()
            .WithEntityAccess())
        {
            ref readonly CharacterStuffPrefab prefabs = ref prefabsRef.ValueRO;
            ref CharacterStuffList weaponsList = ref stuffListRef.ValueRW;

            InstanciateStuff(ecb, prefabs.MeleeWeaponPrefab, chara, ref state, ref weaponsList, StuffType.Melee);
            InstanciateStuff(ecb, prefabs.SecondWeaponPrefab, chara, ref state, ref weaponsList, StuffType.SecondaryWeapon);
            InstanciateStuff(ecb, prefabs.MainWeaponPrefab, chara, ref state, ref weaponsList, StuffType.MainWeapon);

            ecb.RemoveComponent<WaitForInstanciateStuffTag>(chara);
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
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecbEnd = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (ownerRef, stuffInfos, stuff) in SystemAPI
            .Query<RefRO<StuffOwner>, StuffCommonData>()
            .WithAll<PendingStuffTag>()
            .WithEntityAccess())
        {
            var weaponsList = SystemAPI.GetComponentRW<CharacterStuffList>(ownerRef.ValueRO.Value);
            weaponsList.ValueRW.Value[(int)stuffInfos.type] = stuff;

            StuffType stuffInHandType = state.EntityManager.GetComponentData<CharacterStuffInHandType>(ownerRef.ValueRO.Value).Value;
            if (stuffInfos.type == stuffInHandType)
            {
                state.EntityManager.SetComponentEnabled<IsStuffInHand>(weaponsList.ValueRW.Value[(int)stuffInHandType], true);
            }
            ecbEnd.RemoveComponent<PendingStuffTag>(stuff);
        }
    }
}