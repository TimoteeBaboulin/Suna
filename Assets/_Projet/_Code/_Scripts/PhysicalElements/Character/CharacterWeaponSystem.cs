using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public struct StuffIsInstanciedTag : IComponentData { }

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct CharacterWeaponSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<CharacterDefaultWeaponPrefab>().WithAbsent<StuffIsInstanciedTag>();
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
                weapon.ValueRW.Value = ecb.Instantiate(prefab.ValueRO.Value);
                ecb.AddComponent(weapon.ValueRW.Value, new Parent { Value = charaEntity });

                //ecb.AddBuffer<Child>(charaEntity);
                //ecb.AppendToBuffer(charaEntity, new Child { Value = stuffs.ValueRW.mainWeapon });
                ecb.AddComponent(charaEntity, new StuffIsInstanciedTag());
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}