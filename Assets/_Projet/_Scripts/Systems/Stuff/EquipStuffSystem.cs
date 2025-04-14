using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;

[GhostComponent]
public struct EquipStuffQueue : IBufferElementData
{
    [GhostField] public Entity Owner;
    [GhostField] public Entity Stuff;
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

        foreach (var duo in equipStuffQueue)
        {

            if (duo.Owner == Entity.Null || duo.Stuff == Entity.Null) continue;

            var ownerStuffList = SystemAPI.GetComponentRW<CharacterStuffList>(duo.Owner);
            var ownerPos = SystemAPI.GetComponent<LocalToWorld>(duo.Owner).Position;
            int ownerNetworkId = SystemAPI.GetComponent<GhostOwner>(duo.Owner).NetworkId;
            var stuffGhostOwner = SystemAPI.GetComponentRW<GhostOwner>(duo.Stuff);
            var stuffOwner = SystemAPI.GetComponentRW<StuffOwner>(duo.Stuff);
            ref var stuffData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(duo.Stuff).GetData(ref database);

            //If inventory slot is already have stuff, we unequip them
            if (ownerStuffList.ValueRW.GetStuffInSlot(stuffData.slot) != Entity.Null)
            {
                unequipStuffQueue.Add(new UnequipStuffQueue
                {
                    Stuff = ownerStuffList.ValueRW.GetStuffInSlot(stuffData.slot),
                    Owner = duo.Owner,
                    //Position = ownerPos
                });
            }

            //Add the stuff in player inventory
            ownerStuffList.ValueRW.SetStuffInSlot(stuffData.slot, duo.Stuff);

            //Set owner of stuff
            stuffOwner.ValueRW.Value = duo.Owner;

            //Auto switch on new stuff
            //ownerStuffList.ValueRW.StuffInHandSlot = stuffData.slot;

            //Network
            stuffGhostOwner.ValueRW.NetworkId = ownerNetworkId;
            var buffer = SystemAPI.GetBuffer<LinkedEntityGroup>(duo.Owner);
            buffer.Add(new LinkedEntityGroup { Value = duo.Stuff });
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
        var equipStuffQueu = SystemAPI.GetSingletonBuffer<UnequipStuffQueue>();

        foreach (var duo in equipStuffQueu)
        {
            if (duo.Owner == Entity.Null || duo.Stuff == Entity.Null) continue;

            var ownerStuffList = SystemAPI.GetComponentRW<CharacterStuffList>(duo.Owner);
            var stuffOwner = SystemAPI.GetComponentRW<StuffOwner>(duo.Stuff);
            var ownerPos = SystemAPI.GetComponent<LocalToWorld>(duo.Owner).Position;
            var stuffTransform = SystemAPI.GetComponentRW<LocalTransform>(duo.Stuff);
            var stuffGhostOwner = SystemAPI.GetComponentRW<GhostOwner>(duo.Stuff);
            ref var stufData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(duo.Stuff).GetData(ref database);

            if (ownerStuffList.ValueRO.GetStuffInSlot(stufData.slot) == duo.Stuff)
            {
                ownerStuffList.ValueRW.SetStuffInSlot(stufData.slot, Entity.Null);
            }

            stuffOwner.ValueRW.Value = Entity.Null;

            //Network
            stuffGhostOwner.ValueRW.NetworkId = -1;
            var buffer = SystemAPI.GetBuffer<LinkedEntityGroup>(duo.Owner);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Value == duo.Stuff)
                {
                    buffer.RemoveAt(i);
                    break;
                }
            }

            stuffTransform.ValueRW.Position = math.all(duo.Position == float3.zero) ? ownerPos : duo.Position;

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
            foreach (var stuff in charaStuffList.List)
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
                ecb.SetComponent(stuff, new StuffOwner { Value = Entity.Null });
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

