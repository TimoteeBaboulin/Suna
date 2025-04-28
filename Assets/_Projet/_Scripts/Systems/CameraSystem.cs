using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class CameraSystem : SystemBase
{
    private static Entity currentTarget = Entity.Null;
    private static float3 currentPosition;
    private static quaternion currentRotation;
    private static float3 fpsOffset = new float3(0f, 0.9f, 0f);
    private static int defaultFov = 60;
    private static int aimingFov = 50;


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
                        if (currentTarget != Entity.Null
                            && EntityManager.HasComponent<CameraIsAtached>(currentTarget))
                        {
                            EntityManager.RemoveComponent<CameraIsAtached>(currentTarget);
                        }

                        currentTarget = entity;
                        EntityManager.AddComponent<CameraIsAtached>(currentTarget);
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
                if (currentTarget != Entity.Null
                            && EntityManager.HasComponent<CameraIsAtached>(currentTarget))
                {
                    EntityManager.RemoveComponent<CameraIsAtached>(currentTarget);
                }

                currentTarget = entity;
                EntityManager.AddComponent<CameraIsAtached>(currentTarget);
                needNewTarget = false;
            }
        }

        if (needNewTarget)
        {
            if (currentTarget != Entity.Null
                            && EntityManager.HasComponent<CameraIsAtached>(currentTarget))
            {
                EntityManager.RemoveComponent<CameraIsAtached>(currentTarget);
            }

            currentTarget = Entity.Null;

            foreach (var entity in SystemAPI
                .QueryBuilder()
                .WithAll<CharacterTag, CharacterIsEnable>()
                .WithNone<GhostOwnerIsLocal>()
                .Build()
                .ToEntityArray(Allocator.Temp))
            {
                currentTarget = entity;
                EntityManager.AddComponent<CameraIsAtached>(currentTarget);
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

                //bool ADSActive = true;

                //If you can't find the database or the database access of the currently equipped
                //Stuff, you can not ADS
                if (TryGetCurrentlyEquippedStuff(currentTarget, out Entity stuffEntity))
                {
                    CharacterComponent character = EntityManager.GetComponentData<CharacterComponent>(currentTarget);
                    if (character.isAiming && WeaponCanADS(stuffEntity, out float adsFov))
                    {
                        Camera.main.fieldOfView = math.lerp(Camera.main.fieldOfView, adsFov, 0.1f);
                    }
                    else
                    {
                        Camera.main.fieldOfView = math.lerp(Camera.main.fieldOfView, defaultFov, 0.1f);
                    }
                }
                else
                {
                    Camera.main.fieldOfView = math.lerp(Camera.main.fieldOfView, defaultFov, 0.1f);
                }

				Camera.main.transform.position = localTransform.ValueRO.Position + fpsOffset;
                Camera.main.transform.rotation = math.mul(localTransform.ValueRO.Rotation, math.mul(localViewRotation.ValueRO.ViewRotation, localViewRotation.ValueRO.ShootingModifier));
            }
        }
    }

    bool WeaponCanADS(Entity stuffEntity, out float fov)
    {
        fov = 0;

        if (SystemAPI.HasComponent<ScopeComponent>(stuffEntity))
        {
            fov = SystemAPI.GetComponentRO<ScopeComponent>(stuffEntity).ValueRO.ScopeFOV;
            return true; 
        }

        if (SystemAPI.HasComponent<StuffDatabaseAccess>(stuffEntity))
        {
            StuffDatabaseAccess databaseAccess = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffEntity);
            GameResourcesDatabase database = SystemAPI.GetSingleton<GameResourcesDatabase>();

            fov = databaseAccess.GetData(ref database).ADSFOV;
            return databaseAccess.GetData(ref database).canADS;
        }

        return false;
    }

    bool TryGetCurrentlyEquippedStuff(Entity characterEntity, out Entity stuffEntity)
    {
        stuffEntity = default;

        if (!SystemAPI.HasBuffer<CharacterStuffList>(characterEntity))
            return false;

        DynamicBuffer<CharacterStuffList> stuffList = SystemAPI.GetBuffer<CharacterStuffList>(characterEntity);
        CharacterStuffInfos stuffInfos = SystemAPI.GetComponent<CharacterStuffInfos>(characterEntity);
        stuffEntity = StuffUtils.GetStuffInHand(stuffList, stuffInfos);
        return true;
    }
}
