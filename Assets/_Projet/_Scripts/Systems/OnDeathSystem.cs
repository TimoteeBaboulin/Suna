using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateAfter(typeof(OnDieSystem))]
[BurstCompile]
public partial struct OnDeathSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResourcesDatabase>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        SystemAPI.TryGetSingleton<GameResourcesDatabase>(out var database);

        //foreach (var (shouldEmpty, entity) in SystemAPI.Query<RefRO<ShouldEmptyInventory>>().WithEntityAccess())
        //{
        //    for (int i = 0; i < (int)StuffSlot.nbSlots; i++)
        //    {
        //        DynamicBuffer<CharacterStuffList> stuffList = SystemAPI.GetBuffer<CharacterStuffList>(entity);

        //        if (StuffUtils.GetStuffInSlot(stuffList, (StuffSlot)i) != Entity.Null)
        //        {
        //            Debug.Log($"[OnDie] Emptying {StuffUtils.GetStuffInSlot(stuffList, (StuffSlot)i)} from slot {i}");
        //            StuffUtils.SetStuffInSlot(stuffList, (StuffSlot)i, Entity.Null); //Empty the slot
        //        }
        //    }

        //    commandBuffer.RemoveComponent<ShouldEmptyInventory>(entity);
        //}

        foreach (var (shouldDrop, dynamicData, ghostOwner, stuffDatabase, entity) in SystemAPI.Query<RefRO<ShouldBeDropped>, RefRW<StuffDynamicData>, RefRW<GhostOwner>, RefRO<StuffDatabaseAccess>>().WithAll<ShouldBeDropped>().WithEntityAccess())
        {
            if (dynamicData.ValueRO.owner == Entity.Null)
            {
                commandBuffer.RemoveComponent<ShouldBeDropped>(entity);
                continue;
            }

            DynamicBuffer<CharacterStuffList> stuffList = SystemAPI.GetBuffer<CharacterStuffList>(dynamicData.ValueRO.owner);
            DynamicBuffer<LinkedEntityGroup> linkedEntityGroup = SystemAPI.GetBuffer<LinkedEntityGroup>(dynamicData.ValueRO.owner);
            ref StuffCommonData stuffCommonData = ref stuffDatabase.ValueRO.GetData(ref database);
            StuffUtils.Unequip(ref state, dynamicData.ValueRW.owner, linkedEntityGroup, stuffList, entity, ghostOwner, ref stuffCommonData);
            StuffUtils.InstantiateDrop(ref state, ref commandBuffer, entity, shouldDrop.ValueRO.position, shouldDrop.ValueRO.direction, 3f);
            commandBuffer.RemoveComponent<ShouldBeDropped>(entity);
        }

        foreach (var (shouldDelete, dynamicData, ghostOwner, stuffDatabase, entity) in SystemAPI.Query<RefRO<ShouldBeDestroyed>, RefRW<StuffDynamicData>, RefRW<GhostOwner>, RefRO<StuffDatabaseAccess>>().WithAll<ShouldBeDestroyed>().WithEntityAccess())
        {
            if (dynamicData.ValueRO.owner == Entity.Null)
            {
                commandBuffer.RemoveComponent<ShouldBeDestroyed>(entity);
                continue;
            }

            DynamicBuffer<CharacterStuffList> stuffList = SystemAPI.GetBuffer<CharacterStuffList>(dynamicData.ValueRO.owner);
            DynamicBuffer<LinkedEntityGroup> linkedEntityGroup = SystemAPI.GetBuffer<LinkedEntityGroup>(dynamicData.ValueRO.owner);
            ref StuffCommonData stuffCommonData = ref stuffDatabase.ValueRO.GetData(ref database);
            StuffUtils.Unequip(ref state, dynamicData.ValueRW.owner, linkedEntityGroup, stuffList, entity, ghostOwner, ref stuffCommonData);
            commandBuffer.RemoveComponent<ShouldBeDestroyed>(entity);
            commandBuffer.DestroyEntity(entity);
        }

        //UnityEngine.Debug.Log($"[OnDie] PLAYBACK");

        commandBuffer.Playback(state.EntityManager);
        commandBuffer.Dispose();

        foreach (var (shouldDelete, entity) in SystemAPI.Query<RefRO<ShouldBeDropped>>().WithEntityAccess())
        {
            UnityEngine.Debug.Log($"[OnDie] {entity} still has ShouldBeDropped");
        }
    }
}