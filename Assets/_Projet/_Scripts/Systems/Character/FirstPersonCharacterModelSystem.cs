using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct ClientFirstPersonCharacterModelSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (modelPrefab, commonBonesName, ghostOwner, characterEntity) in SystemAPI
            .Query<FirstPersonCharacterModelPrefab, RefRO<CommonCharacterModelBonesName>, RefRO<GhostOwner>>()
            .WithNone<FirstPersonCharacterModelReference>()
            .WithEntityAccess())
        {
            GameObject modelGameObject = CommonCharacterModelUtils.InstantiateModel(modelPrefab.CorpoModelPrefab, 
                modelPrefab.NatifModelPrefab, ghostOwner.ValueRO.NetworkId);

            if (modelPrefab == null) continue;

            CommonCharacterModelUtils.AddCommonModelBonesComponent(modelGameObject.transform, commonBonesName, characterEntity, ecb);
            FirstPersonCharacterModelUtils.AddReferenceComponent(modelGameObject, modelPrefab.DeltaPosition, characterEntity, ecb);   
        }

        foreach (var (characterTransform, modelReference, localViewRotation, commonBonesName, characterEntity) in SystemAPI
            .Query<RefRO<LocalTransform>, FirstPersonCharacterModelReference, RefRO<CharacterViewRotation>, RefRO<CommonCharacterModelBonesName>>()
            .WithEntityAccess())
        {
            if (!state.EntityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
            {
                modelReference.ModelGameObject.SetActive(false);
            }
            else
            {
                if (state.EntityManager.HasComponent<CameraIsAtached>(characterEntity))
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

            quaternion newRotation = math.mul(characterTransform.ValueRO.Rotation, localViewRotation.ValueRO.ViewRotation);
            float3 sd = math.rotate(newRotation, modelReference.ShootDelta);
            float3 newPosition = characterTransform.ValueRO.Position + modelReference.DeltaPosition + sd; //TODO : remove +ShootDelta once we have animations
            CommonCharacterModelUtils.UpdateModelPositionAndRotation(modelReference.ModelGameObject.transform, newPosition, newRotation);
        }

        foreach (var (modelReference, characterEntity) in SystemAPI
            .Query<FirstPersonCharacterModelReference>()
            .WithNone<LocalTransform>()
            .WithEntityAccess())
        {
            FirstPersonCharacterModelUtils.DestroyModel(modelReference.ModelGameObject, characterEntity, ecb);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
