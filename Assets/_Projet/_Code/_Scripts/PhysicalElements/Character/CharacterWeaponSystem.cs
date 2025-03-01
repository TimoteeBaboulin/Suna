using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public struct StuffIsInstanciedTag : IComponentData { }

public struct CharacterWeaponTag : IComponentData { }

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct CharacterWeaponSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<CharacterDefaultWeaponPrefab, CharacterDefaultWeapon>().WithNone<StuffIsInstanciedTag>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (prefab, weapon, charaEntity) in SystemAPI
            .Query<RefRO<CharacterDefaultWeaponPrefab>, RefRW<CharacterDefaultWeapon>>()
            .WithEntityAccess())
        {
            if (prefab.ValueRO.Value != Entity.Null)
            {
                //weapon.ValueRW.Value = ecb.Instantiate(prefab.ValueRO.Value);
                Entity entity = ecb.Instantiate(prefab.ValueRO.Value);
                ecb.AddComponent(entity, new Parent { Value = charaEntity });
                ecb.AddComponent<CharacterDefaultWeaponPrefab>(entity);

                //ecb.AddBuffer<Child>(charaEntity);
                //ecb.AppendToBuffer(charaEntity, new Child { Value = stuffs.ValueRW.mainWeapon });
                ecb.AddComponent(charaEntity, new StuffIsInstanciedTag());
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        foreach (var (parent, entity) in SystemAPI
            .Query<RefRO<Parent>>()
            .WithAll<CharacterWeaponTag>()
            .WithEntityAccess())
        {
            RefRW<CharacterDefaultWeapon> weapon = SystemAPI.GetComponentRW<CharacterDefaultWeapon>(parent.ValueRO.Value);

            if (weapon.ValueRW.Value == Entity.Null)
            {
                weapon.ValueRW.Value = entity;
            }
        }
    }
}