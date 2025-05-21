using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class CameraSystem : SystemBase
{
    private static Entity currentTarget = Entity.Null;
    private static float3 currentPosition;
    private static quaternion currentRotation;
    private static float3 fpsOffset = new float3(0f, 0.8f, 0f);
    public static int defaultFov = 60;
    private static int aimingFov = 50;
    private static int clientId = -1;
    private static CameraController cameraController = null;
    private static bool isSpect = true;
    private static bool updateSpecView = false;
    private static int changeViewIndex = 0;


    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<CharacterTag>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (clientId == -1)
        {
            foreach (var ghostOwner in SystemAPI
                .Query<GhostOwner>()
                .WithAll<ClientComponent, GhostOwnerIsLocal>())
            {
                clientId = ghostOwner.NetworkId;
                break;
            }

            if (clientId == -1)
            {
                return;
            }
        }

        if (cameraController == null)
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }

        TeamSideType teamSide;
        teamSide = PlayerHelpers.GetPlayerInTeam(clientId);

        if (Keyboard.current.eKey.wasPressedThisFrame && teamSide == TeamSideType.Neutre)
        {
            if (!isSpect)
            {
                isSpect = true;
            }
            else
            {
                isSpect = false;

                if (currentTarget != Entity.Null
                    && EntityManager.HasComponent<CameraIsAtached>(currentTarget))
                {
                    EntityManager.RemoveComponent<CameraIsAtached>(currentTarget);
                }

                currentTarget = Entity.Null;
            }
        }

        if (teamSide == TeamSideType.Neutre && !isSpect)
        {
            if (cameraController != null)
            {
                float sensitivity = SystemAPI.GetSingleton<ClientSettingsComponent>().Sensivity;
                cameraController.controllerEnabled = true;
                cameraController.mouseSensitivity = sensitivity * 100;
                Camera.main.fieldOfView = defaultFov;
            }

            return;
        }
        else
        {
            if (cameraController != null)
            {
                cameraController.controllerEnabled = false;
            }
        }

        bool needNewTarget = true;

        if (currentTarget != Entity.Null
            && EntityManager.HasComponent<GhostOwnerIsLocal>(currentTarget)
            && !EntityManager.IsComponentEnabled<GhostOwnerIsLocal>(currentTarget))
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                changeViewIndex -= 1;
                updateSpecView = true;

                if (changeViewIndex < 0)
                {
                    changeViewIndex = 20;
                }
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                changeViewIndex += 1;
                updateSpecView = true;

                if (changeViewIndex > 20)
                {
                    changeViewIndex = 20;
                }
            }

            if (updateSpecView)
            {
                updateSpecView = false;

                NativeList<Entity> playerList = new NativeList<Entity>(Allocator.Temp);

                foreach (var entity in SystemAPI
                    .QueryBuilder()
                    .WithAll<CharacterTag, CharacterIsEnable>()
                    .WithNone<GhostOwnerIsLocal>()
                    .Build()
                    .ToEntityArray(Allocator.Temp))
                {
                    playerList.Add(entity);
                }

                if (playerList.Length != 0)
                {
                    Entity oldTarget = currentTarget;

                    currentTarget = playerList[changeViewIndex % playerList.Length];

                    if (oldTarget != currentTarget)
                    {
                        if (oldTarget != Entity.Null
                        && EntityManager.HasComponent<CameraIsAtached>(oldTarget))
                        {
                            EntityManager.RemoveComponent<CameraIsAtached>(oldTarget);
                        }

                        EntityManager.AddComponent<CameraIsAtached>(currentTarget);
                    }

                    needNewTarget = false;
                }
            }
        }

        if (needNewTarget && currentTarget != Entity.Null && EntityManager.Exists(currentTarget))
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
            NativeList<Entity> playerList = new NativeList<Entity>(Allocator.Temp);

            foreach (var entity in SystemAPI
                .QueryBuilder()
                .WithAll<CharacterTag, CharacterIsEnable>()
                .WithNone<GhostOwnerIsLocal>()
                .Build()
                .ToEntityArray(Allocator.Temp))
            {
                playerList.Add(entity);
            }

            if (playerList.Length != 0)
            {
                Entity oldTarget = currentTarget;

                currentTarget = playerList[changeViewIndex % playerList.Length];

                if (oldTarget != currentTarget)
                {
                    if (currentTarget != Entity.Null
                    && EntityManager.HasComponent<CameraIsAtached>(currentTarget))
                    {
                        EntityManager.RemoveComponent<CameraIsAtached>(currentTarget);
                    }

                    EntityManager.AddComponent<CameraIsAtached>(currentTarget);
                    needNewTarget = false;
                }
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
                        Camera.main.fieldOfView = math.lerp(Camera.main.fieldOfView, adsFov, 0.5f);
                        Camera.main.transform.GetChild(0).gameObject.SetActive(false);
                    }
                    else
                    {
                        Camera.main.fieldOfView = math.lerp(Camera.main.fieldOfView, defaultFov, 0.5f);
                        Camera.main.transform.GetChild(0).gameObject.SetActive(true);
                    }
                }
                else
                {
                    Camera.main.fieldOfView = math.lerp(Camera.main.fieldOfView, defaultFov, 0.1f);
                    Camera.main.transform.GetChild(0).gameObject.SetActive(true);
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