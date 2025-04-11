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
//[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class CameraSystem : SystemBase
{
    private static Entity currentTarget = Entity.Null;
    private static float3 currentPosition;
    private static quaternion currentRotation;
    private static float3 fpsOffset = new float3(0f, 0.9f, 0f);
    private static float3 tpsOffset = new float3(0, 1f, -1f);


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
            if (EntityManager.HasComponent<CharacterIsEnable>(currentTarget)
                && EntityManager.IsComponentEnabled<CharacterIsEnable>(currentTarget))
            {
                if (EntityManager.HasComponent<GhostOwnerIsLocal>(currentTarget)
                    && !EntityManager.IsComponentEnabled<GhostOwnerIsLocal>(currentTarget))
                {
                    foreach (var entity in SystemAPI
                        .QueryBuilder()
                        .WithAll<CharacterTag, CharacterIsEnable, GhostOwnerIsLocal>()
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
                .WithAll<CharacterTag, CharacterIsEnable, GhostOwnerIsLocal>()
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
                .WithAll<CharacterTag, CharacterIsEnable>()
                .WithNone<GhostOwnerIsLocal>()
                .Build()
                .ToEntityArray(Allocator.Temp))
            {
                currentTarget = entity;
                needNewTarget = false;
            }
        }

        if (currentTarget != Entity.Null && EntityManager.Exists(currentTarget))
        {
            if (EntityManager.HasComponent<CharacterIsEnable>(currentTarget)
                && EntityManager.IsComponentEnabled<CharacterIsEnable>(currentTarget)
                && EntityManager.HasComponent<LocalTransform>(currentTarget)
                && EntityManager.HasComponent<CharacterViewRotation>(currentTarget))
            {
                RefRO<LocalTransform> localTransform = SystemAPI.GetComponentRO<LocalTransform>(currentTarget);
                RefRO<CharacterViewRotation> localViewRotation = SystemAPI.GetComponentRO<CharacterViewRotation>(currentTarget);

                if (EntityManager.HasComponent<GhostOwnerIsLocal>(currentTarget)
                && EntityManager.IsComponentEnabled<GhostOwnerIsLocal>(currentTarget))
                {
                    Camera.main.transform.position = localTransform.ValueRO.Position + fpsOffset;
                }
                else
                {
                    Camera.main.transform.position = localTransform.ValueRO.Position + tpsOffset;
                }
                
                Camera.main.transform.rotation = math.mul(localTransform.ValueRO.Rotation, math.mul(localViewRotation.ValueRO.ViewRotation, localViewRotation.ValueRO.ShootingModifier));
            }
        }
    }
}
