using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
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

        TeamSideType teamSide;
        if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
        {
            teamSide = PlayerHelpers.GetPlayerInTeamOnServer(networkId);
        }
        else
        {
            teamSide = PlayerHelpers.GetPlayerInTeam(networkId);
        }

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
        Animator[] allAnimator = model.GetComponentsInChildren<Animator>();

        if (allAnimator.Length == 0) return null; 

        return allAnimator[0];
    }
}
