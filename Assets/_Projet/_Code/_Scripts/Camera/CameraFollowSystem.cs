using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class CameraSystem : SystemBase
{
    private static Entity currentTarget = Entity.Null;
    private static float3 currentPosition;
    private static quaternion currentRotation;

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<CharacterTag>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        bool needNewTarget = true;

        if (currentTarget != Entity.Null && EntityManager.Exists(currentTarget))
        {
            if (!EntityManager.HasComponent<HasNoHealthTag>(currentTarget))
            {
                if (!EntityManager.HasComponent<GhostOwnerIsLocal>(currentTarget))
                {
                    foreach (var entity in SystemAPI
                        .QueryBuilder()
                        .WithAll<CharacterTag, GhostOwnerIsLocal>()
                        .WithNone<HasNoHealthTag>()
                        .Build()
                        .ToEntityArray(Allocator.Temp))
                    {
                        currentTarget = entity;
                        needNewTarget = false;
                    }
                }

                needNewTarget = false;
            }
        }

        if (needNewTarget)
        {
            foreach (var entity in SystemAPI
                .QueryBuilder()
                .WithAll<CharacterTag, GhostOwnerIsLocal>()
                .WithNone<HasNoHealthTag>()
                .Build()
                .ToEntityArray(Allocator.Temp))
            {
                currentTarget = entity;
                needNewTarget = false;
            }
        }

        if (needNewTarget)
        {
            currentTarget = Entity.Null;

            foreach (var entity in SystemAPI
                .QueryBuilder()
                .WithAll<CharacterTag>()
                .WithNone<HasNoHealthTag, GhostOwnerIsLocal>()
                .Build()
                .ToEntityArray(Allocator.Temp))
            {
                currentTarget = entity;
                needNewTarget = false;
            }
        }

        if (currentTarget != Entity.Null && EntityManager.Exists(currentTarget))
        {
            if (!EntityManager.HasComponent<HasNoHealthTag>(currentTarget)
                && EntityManager.HasComponent<LocalTransform>(currentTarget)
                && EntityManager.HasComponent<CharacterLocalViewRotation>(currentTarget))
            {
                RefRO<LocalTransform> localTransform = SystemAPI.GetComponentRO<LocalTransform>(currentTarget);
                RefRO<CharacterLocalViewRotation> localViewRotation = SystemAPI.GetComponentRO<CharacterLocalViewRotation>(currentTarget);

                Camera.main.transform.position = localTransform.ValueRO.Position;
                Camera.main.transform.rotation = math.mul(localTransform.ValueRO.Rotation, localViewRotation.ValueRO.ViewRotation);
            }
        }
    }
}
