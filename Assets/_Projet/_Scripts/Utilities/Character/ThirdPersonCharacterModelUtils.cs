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

    public static void AddModelBonesComponent(in Transform modelTransform, in CharacterColliderBones corpoBones, in CharacterColliderBones natifBones,
        in int networkId, in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        CharacterColliderBones bonesName = null;

        TeamSideType teamSide = PlayerHelpers.GetPlayerInTeam(networkId);

        switch (teamSide)
        {
            case TeamSideType.Corpo:
                bonesName = corpoBones;
                break;
            case TeamSideType.Natif:
                bonesName = natifBones;
                break;
            case TeamSideType.Neutre:
                if ((networkId % 2) == 0)
                {
                    bonesName = corpoBones;
                }
                else
                {
                    bonesName = natifBones;
                }
                break;
        }

        if (bonesName == null) return;

        ecb.AddComponent(characterEntity, new ThirdPersonCharacterModelBonesTransform
        {
            HeadBoneTransform = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.Head),
            ArmLeftBoneTransform0 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.ArmLeft0),
            ArmLeftBoneTransform1 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.ArmLeft1),
            ArmLeftBoneTransform2 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.ArmLeft2),
            ArmRightBoneTransform0 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.ArmRight0),
            ArmRightBoneTransform1 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.ArmRight1),
            ArmRightBoneTransform2 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.ArmRight2),
            ThoraxBoneTransform = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.Thorax),
            StomachBoneTransform0 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.Stomach0),
            StomachBoneTransform1 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.Stomach1),
            LegLeftBoneTransform0 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.LegLeft0),
            LegLeftBoneTransform1 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.LegLeft1),
            LegLeftBoneTransform2 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.LegLeft2),
            LegRightBoneTransform0 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.LegRight0),
            LegRightBoneTransform1 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.LegRight1),
            LegRightBoneTransform2 = CommonCharacterModelUtils.FindBoneByName(modelTransform, bonesName.LegRight2),
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
