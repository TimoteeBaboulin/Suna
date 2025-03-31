using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using static ak.wwise;
using static UnityEngine.UI.GridLayoutGroup;
using UnityEngine;
using static UnityEditor.Progress;
using AK.Wwise;
using Unity.Mathematics;

public struct EquipStuffQueu : IBufferElementData
{
    public Entity Owner;
    public Entity Stuff;
}

public struct UnequipStuffQueu : IBufferElementData
{
    public Entity Owner;
    public Entity Stuff;
    public float3 Position;
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct EquipStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResourcesDatabase>();

        EntityQuery query = state.GetEntityQuery(typeof(EquipStuffQueu));
        query.SetChangedVersionFilter(typeof(EquipStuffQueu));
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();
        var equipStuffQueu = SystemAPI.GetSingletonBuffer<EquipStuffQueu>();
        var unequipStuffQueu = SystemAPI.GetSingletonBuffer<UnequipStuffQueu>();

        foreach (var duo in equipStuffQueu)
        {
            var ownerStuffList = SystemAPI.GetComponentRW<CharacterStuffList>(duo.Owner);
            var stuffOwner = SystemAPI.GetComponentRW<StuffOwner>(duo.Stuff);
            ref var stufData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(duo.Stuff).GetData(ref database);

            unequipStuffQueu.Add(new UnequipStuffQueu
            {
                Stuff = ownerStuffList.ValueRW.Value[(int)stufData.location],
                Owner = duo.Owner
            });

            ownerStuffList.ValueRW.Value[(int)stufData.location] = duo.Stuff;
            stuffOwner.ValueRW.Value = duo.Owner;

            //Attach to Camera View
            Transform viewTransform = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(duo.Owner).WeaponSlotTransform;
            Transform stuffTrasform = state.EntityManager.GetComponentData<StuffGameObjectRef>(duo.Stuff).Value.transform;
            stuffTrasform.SetParent(viewTransform);
            stuffTrasform.localPosition = stufData._stuffLocalOffsetView;
        }
        equipStuffQueu.Clear();
    }
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct UnequipStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResourcesDatabase>();

        EntityQuery query = state.GetEntityQuery(typeof(UnequipStuffQueu));
        query.SetChangedVersionFilter(typeof(UnequipStuffQueu));
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();
        var equipStuffQueu = SystemAPI.GetSingletonBuffer<UnequipStuffQueu>();

        foreach (var duo in equipStuffQueu)
        {
            var ownerStuffList = SystemAPI.GetComponentRW<CharacterStuffList>(duo.Owner);
            var stuffOwner = SystemAPI.GetComponentRW<StuffOwner>(duo.Stuff);
            ref var stufData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(duo.Stuff).GetData(ref database);

            ownerStuffList.ValueRW.Value[(int)stufData.location] = Entity.Null;
            stuffOwner.ValueRW.Value = Entity.Null;

            //Untie Camera View
            Transform stuffTrasform = state.EntityManager.GetComponentData<StuffGameObjectRef>(duo.Stuff).Value.transform;
            stuffTrasform.SetParent(null);
            stuffTrasform.localPosition = default;
            stuffTrasform.gameObject.SetActive(true);
            stuffTrasform.position = duo.Position;

        }
        equipStuffQueu.Clear();
    }
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct StuffOwnershipSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        //Fixes the stuff owner if it doesn't match the player who actually owns it
        foreach (var (charaStuffList, chara) in SystemAPI.Query<CharacterStuffList>().WithEntityAccess())
        {
            foreach (var stuff in charaStuffList.Value)
            {
                if (SystemAPI.Exists(stuff) && SystemAPI.HasComponent<StuffOwner>(stuff))
                {
                    Entity owner = SystemAPI.GetComponent<StuffOwner>(stuff).Value;

                    if (owner != chara)
                    {
                        ecb.SetComponent(stuff, new StuffOwner { Value = chara });
                    }
                }
            }
        }

        //Check if a stuff no longer has a player and set its owner to null
        foreach (var (owner, stuff) in SystemAPI.Query<StuffOwner>().WithEntityAccess())
        {
            bool hasOwner = false;

            foreach (var CharacterStuffList in SystemAPI.Query<CharacterStuffList>())
            {
                foreach (var charaStuff in CharacterStuffList.Value)
                {
                    if (charaStuff == stuff)
                    {
                        hasOwner = true;
                        break;
                    }
                }
            }

            if (!hasOwner)
            {
                ecb.SetComponent(stuff, new StuffOwner { Value = Entity.Null });
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

