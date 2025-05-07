using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Unity.VisualScripting;

public static class StuffUtils
{
    public static void InstantiateNextFrame(DynamicBuffer<InstantiateStuffQueue> instantiateStuffQueue, FixedString128Bytes stuffName, Entity owner)
    {
        if (owner == Entity.Null) return;

        instantiateStuffQueue.Add(new InstantiateStuffQueue
        {
            StuffName = stuffName,
            Owner = owner,
        });
    }

    public static void InstantiateNextFrame(DynamicBuffer<InstantiateStuffQueue> instantiateStuffQueue, FixedString128Bytes stuffName, float3 position)
    {
        instantiateStuffQueue.Add(new InstantiateStuffQueue
        {
            StuffName = stuffName,
            Position = position,
        });
    }

    public static void EquipNextFrame(DynamicBuffer<EquipStuffQueue> equipStuffQueue, Entity owner, Entity stuff, bool autoSwitchOn)
    {
        if (owner == Entity.Null || stuff == Entity.Null) return;

        equipStuffQueue.Add(new EquipStuffQueue
        {
            Stuff = stuff,
            Owner = owner,
            AutoSwitch = autoSwitchOn
        });
    }

    public static void Equip(
        ref SystemState state,
        ref EntityCommandBuffer ecb,
        Entity owner,
        DynamicBuffer<LinkedEntityGroup> linkedEntityGroup,
        RefRO<GhostOwner> ownerGhostOwnerRO,
        DynamicBuffer<CharacterStuffList> ownerStuffList,
        RefRW<CharacterStuffInfos> ownerStuffInfosRW,
        RefRO<CharacterShootStartPositionDelta> shootStartPosDeltaRO,
        RefRO<CharacterViewRotation> ownerViewRO,
        RefRO<LocalTransform> ownerTransformRO,
        Entity stuff,
        ref StuffCommonData stuffData,
        RefRW<StuffDynamicData> stuffDynamicDataRW,
        RefRW<GhostOwner> stuffGhostOwnerRW,
        bool autoSwitchOn)
    {

        if (owner == Entity.Null || stuff == Entity.Null) return;

        if (GetStuffInSlot(ownerStuffList, stuffData.slot) != Entity.Null)
        {
            Entity stuffToDrop = GetStuffInSlot(ownerStuffList, stuffData.slot);
            Drop(
                ref state,
                ref ecb,
                linkedEntityGroup,
                ownerStuffList,
                stuffGhostOwnerRW,
                ref stuffData,
                owner,
                GetStuffInSlot(ownerStuffList, stuffData.slot),
                shootStartPosDeltaRO,
                ownerViewRO,
                ownerTransformRO,
                0f
            );
        }
        // Add the stuff in player inventory
        SetStuffInSlot(ownerStuffList, stuffData.slot, stuff);
        stuffDynamicDataRW.ValueRW.owner = owner;
        stuffDynamicDataRW.ValueRW.dropedEntityRef = Entity.Null;

        // Network
        stuffGhostOwnerRW.ValueRW.NetworkId = ownerGhostOwnerRO.ValueRO.NetworkId;
        linkedEntityGroup.Add(new LinkedEntityGroup { Value = stuff });

        if (autoSwitchOn && stuffData.type != StuffType.Grenade)
        {
            SwitchTo(ownerStuffList, ownerStuffInfosRW, stuffData.slot);
        }
    }

    public static void UnequipNextFrame(DynamicBuffer<UnequipStuffQueue> unequipStuffQueue, Entity owner, Entity stuff)
    {
        if (owner == Entity.Null || stuff == Entity.Null) return;

        unequipStuffQueue.Add(new UnequipStuffQueue
        {
            Stuff = stuff,
            Owner = owner
        });
    }

    public static void Unequip(
        ref SystemState state,
        Entity owner,
        DynamicBuffer<LinkedEntityGroup> linkedEntityGroup,
        DynamicBuffer<CharacterStuffList> ownerStuffList,
        Entity stuff,
        RefRW<GhostOwner> stuffGhostOwnerRW,
        ref StuffCommonData stuffData)
    {
        if (owner == Entity.Null || stuff == Entity.Null) return;

        if (GetStuffInSlot(ownerStuffList, stuffData.slot) == stuff)
        {
            SetStuffInSlot(ownerStuffList, stuffData.slot, Entity.Null);
        }

        var stuffDynamicData = state.EntityManager.GetComponentData<StuffDynamicData>(stuff);
        stuffDynamicData.owner = Entity.Null;
        state.EntityManager.SetComponentData(stuff, stuffDynamicData);

        //Network
        stuffGhostOwnerRW.ValueRW.NetworkId = -1;
        for (int i = 0; i < linkedEntityGroup.Length; i++)
        {
            if (linkedEntityGroup[i].Value == stuff)
            {
                linkedEntityGroup.RemoveAt(i);
                break;
            }
        }
    }

    public static void Throw(
        ref SystemState state,
        DynamicBuffer<LinkedEntityGroup> linkedEntityGroup,
        DynamicBuffer<CharacterStuffList> ownerStuffListRef,
        ref GhostOwner stuffGhostOwnerRef,
        ref StuffDynamicData stuffDynamicDataRef,
        ref StuffCommonData stuffDataRef,
        Entity owner,
        Entity stuff)
    {
        if (owner == Entity.Null || stuff == Entity.Null) return;

        if (GetStuffInSlot(ownerStuffListRef, stuffDataRef.slot) == stuff)
        {
            SetStuffInSlot(ownerStuffListRef, stuffDataRef.slot, Entity.Null);
        }

        var stuffDynamicData = state.EntityManager.GetComponentData<StuffDynamicData>(stuff);
        stuffDynamicData.owner = Entity.Null;
        state.EntityManager.SetComponentData(stuff, stuffDynamicData);

        //Network
        stuffGhostOwnerRef.NetworkId = -1;
        for (int i = 0; i < linkedEntityGroup.Length; i++)
        {
            if (linkedEntityGroup[i].Value == stuff)
            {
                linkedEntityGroup.RemoveAt(i);
                break;
            }
        }
    }

    //public static void DropNextFrame(
    //    ref SystemState state,
    //    DynamicBuffer<UnequipStuffQueue> unequipStuffQueue,
    //    ref EntityCommandBuffer ecb,
    //    Entity owner,
    //    Entity stuff,
    //    RefRO<CharacterShootStartPositionDelta> shootStartPosDelta,
    //    RefRO<CharacterViewRotation> ownerView,
    //    RefRO<LocalTransform> ownerTransform,
    //    float impulse)
    //{
    //    UnequipNextFrame(unequipStuffQueue, owner, stuff);
    //    DropBehavior(ref state, ref ecb, stuff, shootStartPosDelta, ownerView, ownerTransform, impulse);
    //}

    public static void Drop(
        ref SystemState state,
        ref EntityCommandBuffer ecb,
        DynamicBuffer<LinkedEntityGroup> linkedEntityGroup,
        DynamicBuffer<CharacterStuffList> ownerStuffList,
        RefRW<GhostOwner> stuffGhostOwnerRW,
        ref StuffCommonData stuffDataRef,
        Entity owner,
        Entity stuff,
        RefRO<CharacterShootStartPositionDelta> shootStartPosDelta,
        RefRO<CharacterViewRotation> ownerView,
        RefRO<LocalTransform> ownerTransform,
        float impulse)
    {
        Unequip(ref state, owner, linkedEntityGroup, ownerStuffList, stuff, stuffGhostOwnerRW, ref stuffDataRef);
        DropBehavior(ref state, ref ecb, stuff, shootStartPosDelta, ownerView, ownerTransform, impulse);
    }

    private static void DropBehavior(
    ref SystemState state,
    ref EntityCommandBuffer ecb,
    Entity stuff,
    RefRO<CharacterShootStartPositionDelta> shootStartPosDeltaRO,
    RefRO<CharacterViewRotation> ownerViewRO,
    RefRO<LocalTransform> ownerTransformRO,
    float impulse)
    {
        float3 startPosition = shootStartPosDeltaRO.ValueRO.PositionDelta + ownerTransformRO.ValueRO.Position;
        quaternion shootRotation = math.mul(ownerTransformRO.ValueRO.Rotation, ownerViewRO.ValueRO.ViewRotation);
        float3 forward = math.mul(shootRotation, math.forward());

        InstantiateDrop(ref state, ref ecb, stuff, startPosition + forward, forward, impulse);
    }

    public static void InstantiateDrop(
    ref SystemState state,
    ref EntityCommandBuffer ecb,
    Entity stuff,
    float3 startPosition,
    float3 direction,
    float impulse)
    {
        var stuffDynamicData = state.EntityManager.GetComponentData<StuffDynamicData>(stuff);

        Entity dropedStuff = ecb.Instantiate(stuffDynamicData.dropedEntityPrefab);
        ecb.SetName(dropedStuff, "Drop");

        ecb.SetComponent(dropedStuff, new StuffEntityInHandRef { Value = stuff });

        ecb.SetComponent(dropedStuff, new LocalTransform
        {
            Position = startPosition,
            Rotation = quaternion.identity,
            Scale = 1f
        });

        ecb.SetComponent(dropedStuff, new PhysicsVelocity
        {
            Linear = direction * impulse,
            Angular = float3.zero
        });

        stuffDynamicData.dropedEntityRef = dropedStuff;
        ecb.SetComponent(stuff, stuffDynamicData);
    }

    public static void SwitchTo(DynamicBuffer<CharacterStuffList> stuffListe, RefRW<CharacterStuffInfos> stuffInfosRW, StuffSlot slotToSwitch)
    {
        Entity previousStuff = GetStuffInHand(stuffListe, stuffInfosRW.ValueRW);
        Entity nextStuff = GetStuffInSlot(stuffListe, slotToSwitch);

        if (nextStuff == Entity.Null) return;

        stuffInfosRW.ValueRW.StuffInHandSlot = slotToSwitch;
    }

    public static void Destroy(ref SystemState state, Entity stuff)
    {
        state.EntityManager.DestroyEntity(stuff);
    }
    public static Entity GetStuffInHand(DynamicBuffer<CharacterStuffList> stuffList, in CharacterStuffInfos stuffInfos)
    {
        return stuffList[(int)stuffInfos.StuffInHandSlot].entity;
    }
    public static Entity GetStuffInHandUnsafe(ref SystemState state, Entity character)
    {
        var stuffList = state.EntityManager.GetBuffer<CharacterStuffList>(character);
        var stuffInfos = state.EntityManager.GetComponentData<CharacterStuffInfos>(character);

        return stuffList[(int)stuffInfos.StuffInHandSlot].entity;
    }

    public static Entity GetStuffInSlot(DynamicBuffer<CharacterStuffList> stuffListe, StuffSlot slot)
    {
        if (stuffListe.Length > 0)
            return stuffListe[(int)slot].entity;
        else
            return Entity.Null;
    }

    public static void SetStuffInSlot(DynamicBuffer<CharacterStuffList> stuffs, StuffSlot slot, Entity stuff)
    {
        stuffs[(int)slot] = new CharacterStuffList { entity = stuff };
    }

    //Only if stuff is unequip before
    public static void SetStuffViewTransform(StuffGameObjectRef stuffView, LocalTransform transform)
    {
        if (stuffView.Value.transform.parent != null)
        {
            stuffView.Value.transform.SetParent(null);
        }

        stuffView.Value.transform.position = transform.Position;
        stuffView.Value.transform.rotation = transform.Rotation;
        stuffView.Value.transform.localScale = new Vector3(transform.Scale, transform.Scale, transform.Scale);
    }
}
