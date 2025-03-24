using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class CharacterColliderUtils
{
    public static void InstantiateCollider(ref Entity characterCollider,
        in Entity prefab, in Entity character, in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        Entity bodyPartEntity = entityManager.Instantiate(prefab);
        if (entityManager.HasComponent<CharacterColliderDataComponent>(bodyPartEntity))
        {
            var dataComponent = entityManager.GetComponentData<CharacterColliderDataComponent>(bodyPartEntity);
            dataComponent.CharacterEntity = character;
            ecb.SetComponent(bodyPartEntity, dataComponent);
        }
        ecb.AppendToBuffer(character, new LinkedEntityGroup { Value = bodyPartEntity });
        characterCollider = bodyPartEntity;
    }

    public static void SetTransform(in Entity collider, in Transform bone, in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        if (entityManager.HasComponent<LocalTransform>(collider))
        {
            LocalTransform bodyPartTransform = entityManager.GetComponentData<LocalTransform>(collider);
            bodyPartTransform.Position = bone.position;
            bodyPartTransform.Rotation = bone.rotation;
            ecb.SetComponent(collider, bodyPartTransform);
        }
    }
}
