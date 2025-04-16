using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;

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
    [GhostField] public float3 Position;
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
        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();
        var equipStuffQueue = SystemAPI.GetSingletonBuffer<EquipStuffQueue>();
        var unequipStuffQueue = SystemAPI.GetSingletonBuffer<UnequipStuffQueue>();

        foreach (var equipInfos in equipStuffQueue)
        {
            StuffUtilities.EquipUnsafe(ref state, ref database, equipInfos.Owner, equipInfos.Stuff, equipInfos.AutoSwitch);
            //if (equipInfos.Owner == Entity.Null || equipInfos.Stuff == Entity.Null) continue;

            //var ownerStuffList = SystemAPI.GetComponentRW<CharacterStuffList>(equipInfos.Owner);
            //var ownerPos = SystemAPI.GetComponent<LocalToWorld>(equipInfos.Owner).Position;
            //int ownerNetworkId = SystemAPI.GetComponent<GhostOwner>(equipInfos.Owner).NetworkId;
            //var stuffGhostOwner = SystemAPI.GetComponentRW<GhostOwner>(equipInfos.Stuff);
            //var stuffOwner = SystemAPI.GetComponentRW<StuffDynamicData>(equipInfos.Stuff);
            //ref var stuffData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(equipInfos.Stuff).GetData(ref database);

            ////If inventory slot is already have stuff, we unequip them
            //if (ownerStuffList.ValueRW.GetStuffInSlot(stuffData.slot) != Entity.Null)
            //{
            //    unequipStuffQueue.Add(new UnequipStuffQueue
            //    {
            //        Stuff = ownerStuffList.ValueRW.GetStuffInSlot(stuffData.slot),
            //        Owner = equipInfos.Owner,
            //        //Position = ownerPos
            //    });
            //}

            ////Add the stuff in player inventory
            //ownerStuffList.ValueRW.SetStuffInSlot(stuffData.slot, equipInfos.Stuff);

            ////Set owner of stuff
            //stuffOwner.ValueRW.owner = equipInfos.Owner;

            ////Auto switch on new stuff
            ////ownerStuffList.ValueRW.StuffInHandSlot = stuffData.slot;

            ////Network
            //stuffGhostOwner.ValueRW.NetworkId = ownerNetworkId;
            //var buffer = SystemAPI.GetBuffer<LinkedEntityGroup>(equipInfos.Owner);
            //buffer.Add(new LinkedEntityGroup { Value = equipInfos.Stuff });
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
            StuffUtilities.UnequipUnsafe(ref state, ref database, unequipInfos.Owner, unequipInfos.Stuff);

            //if (unequipInfos.Owner == Entity.Null || unequipInfos.Stuff == Entity.Null) continue;

            //var ownerStuffList = SystemAPI.GetComponentRW<CharacterStuffList>(unequipInfos.Owner);
            //var stuffDynamicData = SystemAPI.GetComponentRW<StuffDynamicData>(unequipInfos.Stuff);
            ////var ownerPos = SystemAPI.GetComponent<LocalToWorld>(unequipInfos.Owner).Position;
            //var stuffTransform = SystemAPI.GetComponentRW<LocalTransform>(unequipInfos.Stuff);
            //var stuffGhostOwner = SystemAPI.GetComponentRW<GhostOwner>(unequipInfos.Stuff);
            //ref var stufData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(unequipInfos.Stuff).GetData(ref database);

            //if (ownerStuffList.ValueRO.GetStuffInSlot(stufData.slot) == unequipInfos.Stuff)
            //{
            //    ownerStuffList.ValueRW.SetStuffInSlot(stufData.slot, Entity.Null);
            //}

            //stuffDynamicData.ValueRW.owner = Entity.Null;

            ////Network
            //stuffGhostOwner.ValueRW.NetworkId = -1;
            //var buffer = SystemAPI.GetBuffer<LinkedEntityGroup>(unequipInfos.Owner);
            //for (int i = 0; i < buffer.Length; i++)
            //{
            //    if (buffer[i].Value == unequipInfos.Stuff)
            //    {
            //        buffer.RemoveAt(i);
            //        break;
            //    }
            //}

            //stuffTransform.ValueRW.Position = math.all(unequipInfos.Position == float3.zero) ? ownerPos : unequipInfos.Position;

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

