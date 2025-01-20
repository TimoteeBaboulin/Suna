using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public struct WaitingForRespawnTag : IComponentData { }
public struct PlayerTempsTag : IComponentData { }
public struct NatifTeamTag : IComponentData { }
public struct CorpoTeamTag : IComponentData { }
public struct Health : IComponentData { public int Value; }

public partial struct SpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        foreach (var (health, entity) in SystemAPI.Query<RefRO<Health>>()
            .WithAll<Simulate>()
            .WithAll<PlayerTempsTag>()
            .WithEntityAccess())
        {
            if (entityManager.HasComponent<WaitingForRespawnTag>(entity)) continue;

            ref readonly Health hp = ref health.ValueRO;

            if (hp.Value <= 0)
            {
                entityManager.AddComponent<WaitingForRespawnTag>(entity);
            }
        }
    }
}

//public partial struct RespawnSystem : ISystem
//{
//    public void OnUpdate()
//    {
//        foreach (var waiterLocalTransform in SystemAPI.Query<RefRW<LocalTransform>>()
//            .WithAll<WaitingForRespawnTag>()
//            .WithAll<NatifTeamTag>())
//        {
//            ref LocalTransform waiterTransform = ref waiterLocalTransform.ValueRW;

//            foreach (var (physicsCollider, spawnerComponent, spawnerLocalTransform) in SystemAPI.Query<RefRO<PhysicsCollider>, RefRO<SpawnerComponent>, RefRO<LocalTransform>>()
//                .WithAll<NatifTeamTag>())
//            {
//                ref readonly PhysicsCollider collider = ref physicsCollider.ValueRO;
//                ref readonly SpawnerComponent spawnerData = ref spawnerComponent.ValueRO;
//                ref readonly LocalTransform spawnerTransform = ref spawnerLocalTransform.ValueRO;

                
//            }
//        }
//    }
//}
