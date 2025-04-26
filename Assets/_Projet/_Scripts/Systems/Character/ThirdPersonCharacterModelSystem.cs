using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct ServerThirdPersonCharacterModelSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (modelPrefab, commonBonesName, ghostOwner, characterEntity) in SystemAPI
            .Query<ThirdPersonCharacterModelPrefab, RefRO<CommonCharacterModelBonesName>, RefRO<GhostOwner>>()
            .WithNone<ThirdPersonCharacterModelReference>()
            .WithEntityAccess())
        {
            GameObject modelGameObject = CommonCharacterModelUtils.InstantiateModel(modelPrefab.CorpoModelPrefab,
                modelPrefab.NatifModelPrefab, ghostOwner.ValueRO.NetworkId);

            if (modelPrefab == null) continue;

            CommonCharacterModelUtils.DisableModelRendering(modelGameObject);
            CommonCharacterModelUtils.AddCommonModelBonesComponent(modelGameObject.transform, commonBonesName, characterEntity, ecb);

            ThirdPersonCharacterModelUtils.AddReferenceComponent(modelGameObject, modelPrefab.DeltaPosition, characterEntity, ecb);
            ThirdPersonCharacterModelUtils.AddModelBonesComponent(modelGameObject.transform, modelPrefab.CorpoColliderBones, 
                modelPrefab.NatifColliderBones, ghostOwner.ValueRO.NetworkId, characterEntity, ecb);

            Animator animator = CommonCharacterModelUtils.GetAnimator(modelGameObject);
            AnimationUtils.SetAnimator(animator, characterEntity, ecb, state.EntityManager);
        }

        foreach (var (characterTransform, modelReference, localViewRotation) in SystemAPI
            .Query<RefRO<LocalTransform>, ThirdPersonCharacterModelReference, RefRO<CharacterViewRotation>>())
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
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct ClientThirdPersonCharacterModelSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (modelPrefab, commonBonesName, ghostOwner, characterEntity) in SystemAPI
            .Query<ThirdPersonCharacterModelPrefab, RefRO<CommonCharacterModelBonesName>, RefRO<GhostOwner>>()
            .WithNone<ThirdPersonCharacterModelReference, GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            GameObject modelGameObject = CommonCharacterModelUtils.InstantiateModel(modelPrefab.CorpoModelPrefab,
                modelPrefab.NatifModelPrefab, ghostOwner.ValueRO.NetworkId);

            if (modelPrefab == null) continue;

            CommonCharacterModelUtils.AddCommonModelBonesComponent(modelGameObject.transform, commonBonesName, characterEntity, ecb);

            ThirdPersonCharacterModelUtils.AddReferenceComponent(modelGameObject, modelPrefab.DeltaPosition, characterEntity, ecb);
            ThirdPersonCharacterModelUtils.AddModelBonesComponent(modelGameObject.transform, modelPrefab.CorpoColliderBones,
                modelPrefab.NatifColliderBones, ghostOwner.ValueRO.NetworkId, characterEntity, ecb);
        }

        foreach (var (characterTransform, modelReference, localViewRotation, commonBonesName, characterEntity) in SystemAPI
            .Query<RefRO<LocalTransform>, ThirdPersonCharacterModelReference, RefRO<CharacterViewRotation>, RefRO<CommonCharacterModelBonesName>>()
            .WithEntityAccess())
        {
            if (!state.EntityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
            {
                modelReference.ModelGameObject.SetActive(false);
            }
            else
            {
                if (!state.EntityManager.HasComponent<CameraIsAtached>(characterEntity))
                {
                    CommonCharacterModelUtils.SetCommonModelBonesComponent(modelReference.ModelGameObject.transform, commonBonesName, characterEntity, ecb);

                    Animator animator = CommonCharacterModelUtils.GetAnimator(modelReference.ModelGameObject);
                    AnimationUtils.SetAnimator(animator, characterEntity, ecb, state.EntityManager);

                    if (!modelReference.ModelGameObject.activeSelf)
                    {
                        modelReference.ModelGameObject.SetActive(true);
                    }
                }
                else
                {
                    if (modelReference.ModelGameObject.activeSelf)
                    {
                        modelReference.ModelGameObject.SetActive(false);
                    }
                }
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
