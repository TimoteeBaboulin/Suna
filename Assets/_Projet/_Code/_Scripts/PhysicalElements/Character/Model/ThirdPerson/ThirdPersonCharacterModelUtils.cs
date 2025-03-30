using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ThirdPersonCharacterModelUtils
{
    public static void AddReferenceComponent(in GameObject modelGameObject, float3 modelDeltaPosition, in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        ecb.AddComponent(characterEntity, new ThirdPersonCharacterModelReference
        {
            ModelGameObject = modelGameObject,
            DeltaPosition = modelDeltaPosition,
        });
    }

    public static void AddModelBonesComponent(in Transform modelTransform, RefRO<ThirdPersonCharacterModelBonesName> modelBonesName, 
        in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        ecb.AddComponent(characterEntity, new ThirdPersonCharacterModelBonesTransform
        {
            ViewBoneTransform = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.ViewBoneName),
            HeadBoneTransform = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.HeadBoneName),
            ArmLeftBoneTransform0 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.ArmLeftBoneName0),
            ArmLeftBoneTransform1 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.ArmLeftBoneName1),
            ArmLeftBoneTransform2 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.ArmLeftBoneName2),
            ArmRightBoneTransform0 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.ArmRightBoneName0),
            ArmRightBoneTransform1 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.ArmRightBoneName1),
            ArmRightBoneTransform2 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.ArmRightBoneName2),
            ThoraxBoneTransform = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.ThoraxBoneName),
            StomachBoneTransform0 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.StomachBoneName0),
            StomachBoneTransform1 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.StomachBoneName1),
            LegLeftBoneTransform0 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.LegLeftBoneName0),
            LegLeftBoneTransform1 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.LegLeftBoneName1),
            LegLeftBoneTransform2 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.LegLeftBoneName2),
            LegRightBoneTransform0 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.LegRightBoneName0),
            LegRightBoneTransform1 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.LegRightBoneName1),
            LegRightBoneTransform2 = CommonCharacterModelUtils.FindBoneByName(modelTransform, modelBonesName.ValueRO.LegRightBoneName2),
        });
    }

    public static void UpdateHeadBonesTransform(in GameObject modelGameObject, RefRO<LocalTransform> characterTransform, in quaternion localViewRotation)
    {
        if (modelGameObject.TryGetComponent(out ThirdPersonCharacterModelBehaviour modelBehaviour))
        {
            modelBehaviour.NewHeadRotation = math.mul(characterTransform.ValueRO.Rotation, localViewRotation);
        }
    }

    public static void DestroyModel(in GameObject modelGameObject, in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        Object.Destroy(modelGameObject);
        ecb.RemoveComponent<ThirdPersonCharacterModelReference>(characterEntity);
    }
}
