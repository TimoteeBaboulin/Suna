using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct MainEntityCamera : IComponentData { }

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class MainEntityCameraSystem : SystemBase
{
    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (MainGameObjectCamera.Instance == null)
        {
            return;
            Entity mainEntityCameraEntity = SystemAPI.GetSingletonEntity<MainEntityCamera>();
            LocalToWorld targetLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(mainEntityCameraEntity);
            //MainGameObjectCamera.Instance.transform.SetPositionAndRotation(targetLocalToWorld.Position, targetLocalToWorld.Rotation);
        }

        foreach (RefRO<CameraAttachComponent> cameraAttach in SystemAPI.Query<RefRO<CameraAttachComponent>>().WithAll<GhostOwnerIsLocal>())
        {
            Entity mainEntityCameraEntity = SystemAPI.GetSingletonEntity<MainEntityCamera>();
            LocalToWorld targetLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(mainEntityCameraEntity);
            MainGameObjectCamera.Instance.transform.SetPositionAndRotation(targetLocalToWorld.Position, cameraAttach.ValueRO.transform.Rotation);
        }
    }
}
