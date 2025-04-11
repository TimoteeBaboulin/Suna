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

        foreach (var (modelPrefab, commonBonesName, characterEntity) in SystemAPI
            .Query<FirstPersonCharacterModelPrefab, RefRO<CommonCharacterModelBonesName>>()
            .WithNone<FirstPersonCharacterModelReference>()
            .WithAll<GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            GameObject modelGameObject = Object.Instantiate(modelPrefab.CorpoModelPrefab);

            CommonCharacterModelUtils.AddCommonModelBonesComponent(modelGameObject.transform, commonBonesName, characterEntity, ecb);
            FirstPersonCharacterModelUtils.AddReferenceComponent(modelGameObject, modelPrefab.DeltaPosition, characterEntity, ecb);

            Animator animator = CommonCharacterModelUtils.GetAnimator(modelGameObject);
            AnimationUtils.SetAnimator(animator, characterEntity, ecb, state.EntityManager);
        }

        foreach (var (characterTransform, modelReference, localViewRotation, characterEntity) in SystemAPI
            .Query<RefRO<LocalTransform>, FirstPersonCharacterModelReference, RefRO<CharacterLocalViewRotation>>()
            .WithEntityAccess())
        {
            if (!state.EntityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
            {
                modelReference.ModelGameObject.SetActive(false);
            }
            else
            {
                modelReference.ModelGameObject.SetActive(true);
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
