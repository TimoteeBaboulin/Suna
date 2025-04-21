using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CommonCharacterModelUtils
{
    public static Transform FindBoneByName(in Transform parent, in FixedString64Bytes boneName)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name == boneName)
                return child;
        }
        return null;
    }

    public static GameObject InstantiateModel(in GameObject corpoPrefab, in GameObject natifPrefab, in int networkId)
    {
        GameObject modelGameObject = null;

        TeamSideType teamSide = PlayerHelpers.GetPlayerInTeam(networkId);

        switch (teamSide)
        {
            case TeamSideType.Corpo:
                modelGameObject = Object.Instantiate(corpoPrefab);
                break;
            case TeamSideType.Natif:
                modelGameObject = Object.Instantiate(natifPrefab);
                break;
            case TeamSideType.Neutre:
                if ((networkId % 2) == 0)
                {
                    modelGameObject = Object.Instantiate(corpoPrefab);
                }
                else
                {
                    modelGameObject = Object.Instantiate(natifPrefab);
                }
                break;
        }

        return modelGameObject;
    }

    public static void AddCommonModelBonesComponent(in Transform modelTransform, RefRO<CommonCharacterModelBonesName> modelBonesName,
        in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        ecb.AddComponent(characterEntity, new CommonCharacterModelBonesTransform
        {
            WeaponSlotTransform = FindBoneByName(modelTransform, modelBonesName.ValueRO.WeaponSlotName),
        });
    }

    public static void SetCommonModelBonesComponent(in Transform modelTransform, RefRO<CommonCharacterModelBonesName> modelBonesName,
        in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        ecb.SetComponent(characterEntity, new CommonCharacterModelBonesTransform
        {
            WeaponSlotTransform = FindBoneByName(modelTransform, modelBonesName.ValueRO.WeaponSlotName),
        });
    }

    public static void DisableModelRendering(in GameObject modelGameObject)
    {
        if (modelGameObject.TryGetComponent(out ThirdPersonCharacterModelBehaviour modelBehaviour)
            && modelBehaviour.MeshRenderer.enabled)
        {
            modelBehaviour.MeshRenderer.enabled = false;
        }
    }

    public static void UpdateModelPositionAndRotation(in Transform modelTransform, in float3 newPosition, in quaternion newRotation)
    {
        modelTransform.position = newPosition;
        modelTransform.transform.rotation = newRotation;
    }

    public static Animator GetAnimator(in GameObject model)
    {
        if (model.TryGetComponent(out Animator animator))
        {
            return animator;
        }

        return null;
    }
}
