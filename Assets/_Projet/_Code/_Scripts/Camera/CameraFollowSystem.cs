using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class CameraSystem : SystemBase
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
        }

        foreach (var transform in SystemAPI
            .Query<RefRO<LocalTransform>>()
            .WithAll<CameraFollowIsEnable>())
        {
            MainGameObjectCamera.Instance.transform.position = transform.ValueRO.Position;
            MainGameObjectCamera.Instance.transform.rotation = transform.ValueRO.Rotation;
            break;
        }
    }
}
