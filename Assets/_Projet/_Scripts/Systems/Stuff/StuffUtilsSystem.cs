using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

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
            StuffUtils.EquipUnsafe(ref ecb, ref state, ref database, equipInfos.Owner, equipInfos.Stuff, true);

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
            StuffUtils.UnequipUnsafe(ref state, ref database, unequipInfos.Owner, unequipInfos.Stuff);
        }
        unequipStuffQueu.Clear();
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
            foreach (var stuff in charaStuffList.List)
            {
                if (SystemAPI.Exists(stuff) && SystemAPI.HasComponent<StuffDynamicData>(stuff))
                {
                    Entity owner = SystemAPI.GetComponent<StuffDynamicData>(stuff).owner;

                    if (owner != chara)
                    {
                        var dynData = SystemAPI.GetComponent<StuffDynamicData>(stuff);
                        dynData.owner = chara;
                        ecb.SetComponent(stuff, dynData);
                    }
                }
            }
        }

        //Check if a stuff no longer has a player and set its owner to null
        foreach (var (owner, stuff) in SystemAPI.Query<StuffDynamicData>().WithEntityAccess())
        {
            bool hasOwner = false;

            foreach (var CharacterStuffList in SystemAPI.Query<CharacterStuffList>())
            {
                foreach (var charaStuff in CharacterStuffList.List)
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
                var dynData = SystemAPI.GetComponent<StuffDynamicData>(stuff);
                dynData.owner = Entity.Null;
                ecb.SetComponent(stuff, dynData);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct StuffDropedCleanup : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Get ECB
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (stuffInHandRef, dropedStuff) in SystemAPI.Query<RefRO<StuffEntityInHandRef>>().WithEntityAccess())
        {
            if (stuffInHandRef.ValueRO.Value == Entity.Null)
            {
                ecb.DestroyEntity(dropedStuff);
            }
        }
    }
}

