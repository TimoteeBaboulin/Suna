using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct ServerThirdPersonCharacterModelSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (modelPrefab, modelBonesName, commonBonesName, characterEntity) in SystemAPI
            .Query<ThirdPersonCharacterModelPrefab, RefRO<ThirdPersonCharacterModelBonesName>, RefRO<CommonCharacterModelBonesName>>()
            .WithNone<ThirdPersonCharacterModelReference>()
            .WithEntityAccess())
        {
            GameObject modelGameObject = Object.Instantiate(modelPrefab.CorpoModelPrefab);

            CommonCharacterModelUtils.DisableModelRendering(modelGameObject);
            CommonCharacterModelUtils.AddCommonModelBonesComponent(modelGameObject.transform, commonBonesName, characterEntity, ecb);

            ThirdPersonCharacterModelUtils.AddReferenceComponent(modelGameObject, modelPrefab.DeltaPosition, characterEntity, ecb);
            ThirdPersonCharacterModelUtils.AddModelBonesComponent(modelGameObject.transform, modelBonesName, characterEntity, ecb);
        }

        foreach (var (characterTransform, modelReference, localViewRotation) in SystemAPI
            .Query<RefRO<LocalTransform>, ThirdPersonCharacterModelReference, RefRO<CharacterLocalViewRotation>>())
        {
            float3 newPosition = characterTransform.ValueRO.Position + modelReference.DeltaPosition;
            quaternion newRotation = characterTransform.ValueRO.Rotation;
            CommonCharacterModelUtils.UpdateModelPositionAndRotation(modelReference.ModelGameObject.transform, newPosition, newRotation);

            ThirdPersonCharacterModelUtils.UpdateHeadBonesTransform(modelReference.ModelGameObject, characterTransform, localViewRotation.ValueRO.ViewRotation);
        }

        foreach (var (modelReference, characterEntity) in SystemAPI
            .Query<ThirdPersonCharacterModelReference>()
            .WithNone<LocalTransform>()
            .WithEntityAccess())
        {
            ThirdPersonCharacterModelUtils.DestroyModel(modelReference.ModelGameObject, characterEntity, ecb);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct ClientThirdPersonCharacterModelSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (modelPrefab, modelBonesName, commonBonesName, characterEntity) in SystemAPI
            .Query<ThirdPersonCharacterModelPrefab, RefRO<ThirdPersonCharacterModelBonesName>, RefRO<CommonCharacterModelBonesName>>()
            .WithNone<ThirdPersonCharacterModelReference, GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            GameObject modelGameObject = Object.Instantiate(modelPrefab.CorpoModelPrefab);

            CommonCharacterModelUtils.AddCommonModelBonesComponent(modelGameObject.transform, commonBonesName, characterEntity, ecb);

            ThirdPersonCharacterModelUtils.AddReferenceComponent(modelGameObject, modelPrefab.DeltaPosition, characterEntity, ecb);
            ThirdPersonCharacterModelUtils.AddModelBonesComponent(modelGameObject.transform, modelBonesName, characterEntity, ecb);
        }

        foreach (var (characterTransform, modelReference, localViewRotation, characterEntity) in SystemAPI
            .Query<RefRO<LocalTransform>, ThirdPersonCharacterModelReference, RefRO<CharacterLocalViewRotation>>()
            .WithEntityAccess())
        {
            if (!state.EntityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
            {
                CommonCharacterModelUtils.DisableModelRendering(modelReference.ModelGameObject);
            }
            else
            {
                CommonCharacterModelUtils.EnableModelRendering(modelReference.ModelGameObject);
            }

            float3 newPosition = characterTransform.ValueRO.Position + modelReference.DeltaPosition;
            quaternion newRotation = characterTransform.ValueRO.Rotation;
            CommonCharacterModelUtils.UpdateModelPositionAndRotation(modelReference.ModelGameObject.transform, newPosition, newRotation);

            ThirdPersonCharacterModelUtils.UpdateHeadBonesTransform(modelReference.ModelGameObject, characterTransform, localViewRotation.ValueRO.ViewRotation);
        }

        foreach (var (modelReference, entity) in SystemAPI
            .Query<ThirdPersonCharacterModelReference>()
            .WithNone<LocalTransform>()
            .WithEntityAccess())
        {
            Object.Destroy(modelReference.ModelGameObject);
            ecb.RemoveComponent<ThirdPersonCharacterModelReference>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
