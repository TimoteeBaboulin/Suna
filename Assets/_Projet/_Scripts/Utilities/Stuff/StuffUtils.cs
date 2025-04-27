using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

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

    public static void EquipUnsafe(ref EntityCommandBuffer ecb, ref SystemState state, ref GameResourcesDatabase database, Entity owner, Entity stuff, bool autoSwitchOn)
    {
        if (owner == Entity.Null || stuff == Entity.Null) return;

        var ownerStuffList = state.EntityManager.GetBuffer<CharacterStuffList>(owner);
        var ownerStuffInfos = state.EntityManager.GetComponentData<CharacterStuffInfos>(owner);
        var stuffGhostOwner = state.EntityManager.GetComponentData<GhostOwner>(stuff);
        var stuffDynamicData = state.EntityManager.GetComponentData<StuffDynamicData>(stuff);
        var ownerGhostOwner = state.EntityManager.GetComponentData<GhostOwner>(owner);
        ref var stuffData = ref state.EntityManager.GetComponentData<StuffDatabaseAccess>(stuff).GetData(ref database);
        var linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(owner);

        var shootStartPosDelta = state.EntityManager.GetComponentData<CharacterShootStartPositionDelta>(owner);
        var ownerView = state.EntityManager.GetComponentData<CharacterViewRotation>(owner);
        var ownerTransform = state.EntityManager.GetComponentData<LocalTransform>(owner);

        Equip(ref state, linkedEntityGroup, ownerGhostOwner, ownerStuffList, ref ownerStuffInfos, ref stuffGhostOwner, ref stuffDynamicData, ref stuffData, owner, stuff, ref ecb, shootStartPosDelta, ownerView, ownerTransform, autoSwitchOn);

        state.EntityManager.SetComponentData(stuff, stuffGhostOwner);
        state.EntityManager.SetComponentData(stuff, stuffDynamicData);

    }

    public static void Equip(
        ref SystemState state,
        DynamicBuffer<LinkedEntityGroup> linkedEntityGroup,
        GhostOwner ownerGhostOwner,
        DynamicBuffer<CharacterStuffList> ownerStuffListRef,
        ref CharacterStuffInfos ownerStuffInfos,
        ref GhostOwner stuffGhostOwnerRef,
        ref StuffDynamicData stuffDynamicDataRef,
        ref StuffCommonData stuffDataRef,
        Entity owner,
        Entity stuff,
        ref EntityCommandBuffer ecb,
        CharacterShootStartPositionDelta shootStartPosDelta,
        CharacterViewRotation ownerView,
        LocalTransform ownerTransform,
        bool autoSwitchOn)
    {

        if (owner == Entity.Null || stuff == Entity.Null) return;

        if (GetStuffInSlot(ownerStuffListRef, stuffDataRef.slot) != Entity.Null)
        {
            Drop(
                ref ecb,
                linkedEntityGroup,
                ref ownerStuffListRef,
                ref stuffGhostOwnerRef,
                ref stuffDynamicDataRef,
                ref stuffDataRef,
                owner,
                GetStuffInSlot(ownerStuffListRef, stuffDataRef.slot),
                shootStartPosDelta,
                ownerView,
                ownerTransform,
                0f
            );
        }
        // Add the stuff in player inventory
        SetStuffInSlot(ownerStuffListRef, stuffDataRef.slot, stuff);

        // Set owner of stuff
        stuffDynamicDataRef.owner = owner;

        // Network
        stuffGhostOwnerRef.NetworkId = ownerGhostOwner.NetworkId;
        linkedEntityGroup.Add(new LinkedEntityGroup { Value = stuff });

        if (autoSwitchOn)
            SwitchTo(ownerStuffListRef, ref ownerStuffInfos, stuffDataRef.slot);
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

    public static void UnequipUnsafe(ref SystemState state, ref GameResourcesDatabase database, Entity owner, Entity stuff)
    {
        if (owner == Entity.Null || stuff == Entity.Null) return;

        var linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(owner);
        var ownerStuffList = state.EntityManager.GetBuffer<CharacterStuffList>(owner);
        var stuffGhostOwner = state.EntityManager.GetComponentData<GhostOwner>(stuff);
        var stuffDynamicData = state.EntityManager.GetComponentData<StuffDynamicData>(stuff);
        ref var stuffData = ref state.EntityManager.GetComponentData<StuffDatabaseAccess>(stuff).GetData(ref database);

        Unequip(linkedEntityGroup, ownerStuffList, ref stuffGhostOwner, ref stuffDynamicData, ref stuffData, owner, stuff);

        state.EntityManager.SetComponentData(stuff, stuffGhostOwner);
        state.EntityManager.SetComponentData(stuff, stuffDynamicData);
    }

    public static void Unequip(
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

        stuffDynamicDataRef.owner = Entity.Null;
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

    public static void DropNextFrame(
        DynamicBuffer<UnequipStuffQueue> unequipStuffQueue,
        ref EntityCommandBuffer ecb,
        ref StuffDynamicData stuffDynamicDataRef,
        Entity owner,
        Entity stuff,
        CharacterShootStartPositionDelta shootStartPosDelta,
        CharacterViewRotation ownerView,
        LocalTransform ownerTransform,
        float impulse)
    {
        UnequipNextFrame(unequipStuffQueue, owner, stuff);
        DropBehavior(ref ecb, ref stuffDynamicDataRef, stuff, shootStartPosDelta, ownerView, ownerTransform, impulse);
    }

    //public static void DropUnsafe(ref SystemState state, ref EntityCommandBuffer ecb, ref GameResourcesDatabase database, Entity owner, Entity stuff, float impulse)
    //{
    //    var linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(owner);
    //    var ownerStuffList = state.EntityManager.GetBuffer<CharacterStuffList>(owner);
    //    var shootStartPosDelta = state.EntityManager.GetComponentData<CharacterShootStartPositionDelta>(owner);
    //    var ownerView = state.EntityManager.GetComponentData<CharacterViewRotation>(owner);
    //    var ownerTransform = state.EntityManager.GetComponentData<LocalTransform>(owner);
    //    var stuffGhostOwner = state.EntityManager.GetComponentData<GhostOwner>(stuff);
    //    var stuffDynamicData = state.EntityManager.GetComponentData<StuffDynamicData>(stuff);
    //    ref var stuffData = ref state.EntityManager.GetComponentData<StuffDatabaseAccess>(stuff).GetData(ref database);

    //    Unequip(linkedEntityGroup, ref ownerStuffList, ref stuffGhostOwner, ref stuffDynamicData, ref stuffData, owner, stuff);
    //    DropBehavior(ref ecb, ref stuffDynamicData, stuff, shootStartPosDelta, ownerView, ownerTransform, impulse);

    //    state.EntityManager.SetComponentData(owner, ownerStuffList);
    //    state.EntityManager.Buffer;
    //    state.EntityManager.SetComponentData(stuff, stuffGhostOwner);
    //    state.EntityManager.SetComponentData(stuff, stuffDynamicData);

    //}
    public static void Drop(
        ref EntityCommandBuffer ecb,
        DynamicBuffer<LinkedEntityGroup> linkedEntityGroup,
        ref DynamicBuffer<CharacterStuffList> ownerStuffListRef,
        ref GhostOwner stuffGhostOwnerRef,
        ref StuffDynamicData stuffDynamicDataRef,
        ref StuffCommonData stuffDataRef,
        Entity owner,
        Entity stuff,
        CharacterShootStartPositionDelta shootStartPosDelta,
        CharacterViewRotation ownerView,
        LocalTransform ownerTransform,
        float impulse)
    {
        Unequip(linkedEntityGroup, ownerStuffListRef, ref stuffGhostOwnerRef, ref stuffDynamicDataRef, ref stuffDataRef, owner, stuff);
        DropBehavior(ref ecb, ref stuffDynamicDataRef, stuff, shootStartPosDelta, ownerView, ownerTransform, impulse);
    }

    private static void DropBehavior(
    ref EntityCommandBuffer ecb,
    ref StuffDynamicData stuffDynamicDataRef,
    Entity stuff,
    CharacterShootStartPositionDelta shootStartPosDelta,
    CharacterViewRotation ownerView,
    LocalTransform ownerTransform,
    float impulse)
    {
        float3 startPosition = shootStartPosDelta.PositionDelta + ownerTransform.Position;
        quaternion shootRotation = math.mul(ownerTransform.Rotation, ownerView.ViewRotation);
        float3 forward = math.mul(shootRotation, math.forward());

        InstantiateDrop(ref ecb, ref stuffDynamicDataRef, stuff, startPosition + forward, forward, impulse);
    }

    public static void InstantiateDrop(
    ref EntityCommandBuffer ecb,
    ref StuffDynamicData stuffDynamicDataRef,
    Entity stuff,
    float3 startPosition,
    float3 direction,
    float impulse)
    {
        Entity dropedStuff = ecb.Instantiate(stuffDynamicDataRef.dropedEntityPrefab);

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
    }

    public static void SwitchTo(DynamicBuffer<CharacterStuffList> stuffListe, ref CharacterStuffInfos stuffInfos, StuffSlot slotToSwitch)
    {
        Entity previousStuff = GetStuffInHand(stuffListe, stuffInfos);
        Entity nextStuff = GetStuffInSlot(stuffListe, slotToSwitch);

        if (nextStuff == Entity.Null) return;

        stuffInfos.StuffInHandSlot = slotToSwitch;
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
        return stuffListe[(int)slot].entity;
    }

    private static void SetStuffInSlot(DynamicBuffer<CharacterStuffList> stuffs, StuffSlot slot, Entity stuff)
    {
        if (stuffs[(int)slot].entity == Entity.Null)
        {
            stuffs[(int)slot] = new CharacterStuffList { entity = stuff };
        }
    }
}
