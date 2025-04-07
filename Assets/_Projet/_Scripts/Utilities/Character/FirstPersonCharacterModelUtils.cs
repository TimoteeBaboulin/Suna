using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FirstPersonCharacterModelUtils
{
    public static void AddReferenceComponent(in GameObject modelGameObject, float3 modelDeltaPosition, in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        ecb.AddComponent(characterEntity, new FirstPersonCharacterModelReference
        {
            ModelGameObject = modelGameObject,
            DeltaPosition = modelDeltaPosition,
        });
    }

    public static void DestroyModel(in GameObject modelGameObject, in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        Object.Destroy(modelGameObject);
        ecb.RemoveComponent<FirstPersonCharacterModelReference>(characterEntity);
    }
}
