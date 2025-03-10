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

        foreach (var characterModelBones in SystemAPI.Query<CharacterModelBones>().WithAll<GhostOwnerIsLocal>())
        {
            MainGameObjectCamera.Instance.transform.position = characterModelBones.ViewBoneTransform.position;
            MainGameObjectCamera.Instance.transform.rotation = characterModelBones.ViewBoneTransform.rotation;
        }
    }
}
