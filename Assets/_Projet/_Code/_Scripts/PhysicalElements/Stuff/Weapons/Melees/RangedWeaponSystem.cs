using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public partial struct ReloadWeaponSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (rangedWeaponComponent, localTransform)
            in SystemAPI.Query<RefRW<RangedWeaponComponent>, RefRO<LocalTransform>>())
        {
            ref RangedWeaponComponent data = ref rangedWeaponComponent.ValueRW;
            ref readonly LocalTransform transform = ref localTransform.ValueRO;

            //CONTENT
        }
    }
}

