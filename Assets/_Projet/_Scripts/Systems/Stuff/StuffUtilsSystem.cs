using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Unity.Transforms;

[GhostComponent]
public struct EquipStuffQueue : IBufferElementData
{
    [GhostField] public Entity Owner;
    [GhostField] public Entity Stuff;
    [GhostField] public bool AutoSwitch;
}

[GhostComponent]
public struct UnequipStuffQueue : IBufferElementData
{
    [GhostField] public Entity Owner;
    [GhostField] public Entity Stuff;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateAfter(typeof(UnequipStuffSystem))]
public partial struct EquipStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResourcesDatabase>();

        EntityQuery query = state.GetEntityQuery(typeof(EquipStuffQueue));
        query.SetChangedVersionFilter(typeof(EquipStuffQueue));
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();
        var equipStuffQueue = SystemAPI.GetSingletonBuffer<EquipStuffQueue>();
        var unequipStuffQueue = SystemAPI.GetSingletonBuffer<UnequipStuffQueue>();

        foreach (var equipInfos in equipStuffQueue)
        {
            //Owner
            var linkedEntityGroup = SystemAPI.GetBuffer<LinkedEntityGroup>(equipInfos.Owner);
            var ownerGhostOwnerRO = SystemAPI.GetComponentRO<GhostOwner>(equipInfos.Owner);
            var ownerStuffList = SystemAPI.GetBuffer<CharacterStuffList>(equipInfos.Owner);
            var ownerStuffInfosRW = SystemAPI.GetComponentRW<CharacterStuffInfos>(equipInfos.Owner);
            var shootStartPosDeltaRO = SystemAPI.GetComponentRO<CharacterShootStartPositionDelta>(equipInfos.Owner);
            var ownerViewRO = SystemAPI.GetComponentRO<CharacterViewRotation>(equipInfos.Owner);
            var ownerTransformRO = SystemAPI.GetComponentRO<LocalTransform>(equipInfos.Owner);

            //Stuff
            ref var stuffData = ref SystemAPI.GetComponentRO<StuffDatabaseAccess>(equipInfos.Stuff).ValueRO.GetData(ref database);
            var stuffDynamicDataRW = SystemAPI.GetComponentRW<StuffDynamicData>(equipInfos.Stuff);
            var stuffGhostOwnerRW = SystemAPI.GetComponentRW<GhostOwner>(equipInfos.Stuff);

            StuffUtils.Equip(ref state, ref ecb, equipInfos.Owner, linkedEntityGroup, ownerGhostOwnerRO, ownerStuffList,ownerStuffInfosRW, 
                shootStartPosDeltaRO, ownerViewRO, ownerTransformRO, equipInfos.Stuff, ref stuffData, stuffDynamicDataRW, stuffGhostOwnerRW, equipInfos.AutoSwitch);
        }
        equipStuffQueue.Clear();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateBefore(typeof(EquipStuffSystem))]
public partial struct UnequipStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResourcesDatabase>();

        EntityQuery query = state.GetEntityQuery(typeof(UnequipStuffQueue));
        query.SetChangedVersionFilter(typeof(UnequipStuffQueue));
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();
        var unequipStuffQueu = SystemAPI.GetSingletonBuffer<UnequipStuffQueue>();

        foreach (var unequipInfos in unequipStuffQueu)
        {
            //Owner
            var linkedEntityGroup = SystemAPI.GetBuffer<LinkedEntityGroup>(unequipInfos.Owner);
            var ownerStuffList = SystemAPI.GetBuffer<CharacterStuffList>(unequipInfos.Owner);

            //Stuff
            var stuffGhostOwnerRW = SystemAPI.GetComponentRW<GhostOwner>(unequipInfos.Stuff);
            var stuffDynamicDataRW = SystemAPI.GetComponentRW<StuffDynamicData>(unequipInfos.Stuff);
            ref var stuffData = ref SystemAPI.GetComponentRO<StuffDatabaseAccess>(unequipInfos.Stuff).ValueRO.GetData(ref database);

            StuffUtils.Unequip(ref state, unequipInfos.Owner, linkedEntityGroup, ownerStuffList, 
                unequipInfos.Stuff, stuffGhostOwnerRW, ref stuffData);
        }
        unequipStuffQueu.Clear();
    }
}

//[BurstCompile]
//[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
//public partial struct StuffOwnershipSystem : ISystem
//{
//    public void OnUpdate(ref SystemState state)
//    {
//        var ecb = new EntityCommandBuffer(Allocator.Temp);

//        //Fixes the stuff owner if it doesn't match the player who actually owns it
//        foreach (var (charaStuffList, chara) in SystemAPI.Query<DynamicBuffer<CharacterStuffList>>().WithEntityAccess())
//        {
//            foreach (var stuff in charaStuffList)
//            {
//                if (SystemAPI.Exists(stuff.entity) && SystemAPI.HasComponent<StuffDynamicData>(stuff.entity))
//                {
//                    Entity owner = SystemAPI.GetComponent<StuffDynamicData>(stuff.entity).ownerTest;

//                    if (owner != chara)
//                    {
//                        var dynData = SystemAPI.GetComponent<StuffDynamicData>(stuff.entity);
//                        dynData.ownerTest = chara;
//                        ecb.SetComponent(stuff.entity, dynData);
//                    }
//                }
//            }
//        }
//    }
//}



