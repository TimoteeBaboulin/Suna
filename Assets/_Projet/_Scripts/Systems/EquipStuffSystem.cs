using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[GhostComponent]
public struct EquipStuffQueu : IBufferElementData
{
    [GhostField] public Entity Owner;
    [GhostField] public Entity Stuff;
}

[GhostComponent]
public struct UnequipStuffQueu : IBufferElementData
{
    [GhostField] public Entity Owner;
    [GhostField] public Entity Stuff;
    [GhostField] public float3 Position;
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
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
            if (duo.Owner == Entity.Null || duo.Stuff == Entity.Null) continue;

            var ownerStuffList = SystemAPI.GetComponentRW<CharacterStuffList>(duo.Owner);
            var ownerPos = SystemAPI.GetComponent<LocalToWorld>(duo.Owner).Position;
            var stuffOwner = SystemAPI.GetComponentRW<StuffOwner>(duo.Stuff);
            ref var stufData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(duo.Stuff).GetData(ref database);

            if (ownerStuffList.ValueRW.Value[(int)stufData.location] != Entity.Null)
            {
                unequipStuffQueu.Add(new UnequipStuffQueu
                {
                    Stuff = ownerStuffList.ValueRW.Value[(int)stufData.location],
                    Owner = duo.Owner,
                    Position = ownerPos
                });
            }

            ownerStuffList.ValueRW.Value[(int)stufData.location] = duo.Stuff;
            stuffOwner.ValueRW.Value = duo.Owner;

            //Attach to Camera View
            if (state.World.IsClient()) //Probléme IsClient ne s'actualise pas corectement, à deplacer dans un system dédié
            {
                Transform viewTransform = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(duo.Owner).WeaponSlotTransform;
                Transform stuffTrasform = state.EntityManager.GetComponentData<StuffGameObjectRef>(duo.Stuff).Value.transform;
                stuffTrasform.SetParent(viewTransform);
                stuffTrasform.localPosition = stufData._stuffLocalOffsetView;
            }
        }
        equipStuffQueu.Clear();
    }
}


[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateBefore(typeof(EquipStuffSystem))]
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
            if (duo.Owner == Entity.Null || duo.Stuff == Entity.Null) continue;

            var ownerStuffList = SystemAPI.GetComponentRW<CharacterStuffList>(duo.Owner);
            var stuffOwner = SystemAPI.GetComponentRW<StuffOwner>(duo.Stuff);
            ref var stufData = ref SystemAPI.GetComponent<StuffDatabaseAccess>(duo.Stuff).GetData(ref database);

            ownerStuffList.ValueRW.Value[(int)stufData.location] = Entity.Null;
            stuffOwner.ValueRW.Value = Entity.Null;

            //Untie Camera View
            if (state.World.IsClient()) //Probléme IsClient ne s'actualise pas corectement, à deplacer dans un system dédié
            {
                Transform stuffTrasform = state.EntityManager.GetComponentData<StuffGameObjectRef>(duo.Stuff).Value.transform;
                stuffTrasform.SetParent(null);
                stuffTrasform.localPosition = default;
                stuffTrasform.gameObject.SetActive(true);
                stuffTrasform.position = duo.Position;
            }
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

