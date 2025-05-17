using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class FirstPersonCharacterModelUtils
{
    public static void AddReferenceComponent(in GameObject modelGameObject, FirstPersonCharacterModelPrefab modelPrefab, 
        in Entity characterEntity, in EntityCommandBuffer ecb, TeamSideType teamSide)
    {
        ModelAnimatorData animatorData = null;

        switch (teamSide)
        {
            case TeamSideType.Corpo:
                animatorData = modelPrefab.CorpoAnimatorData;
                break;
            case TeamSideType.Natif:
                animatorData = modelPrefab.NatifAnimatorData;
                break;
        }

        ecb.AddComponent(characterEntity, new FirstPersonCharacterModelReference
        {
            ModelGameObject = modelGameObject,
            AnimatorData = animatorData,
            DeltaPosition = modelPrefab.DeltaPosition,
        });
    }

    public static void DestroyModel(in GameObject modelGameObject, in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        Object.Destroy(modelGameObject);
        ecb.RemoveComponent<FirstPersonCharacterModelReference>(characterEntity);
    }
}
