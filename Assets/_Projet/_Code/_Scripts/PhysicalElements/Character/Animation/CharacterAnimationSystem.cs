using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct CharacterAnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterGameObjectPrefab>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (characterGameObjectPrefab, entity) in SystemAPI
            .Query<CharacterGameObjectPrefab>()
            .WithNone<CharacterAnimatorReference>()
            .WithEntityAccess())
        {
            GameObject newGameObject = Object.Instantiate(characterGameObjectPrefab.GameObjectPrefab);
            ecb.AddComponent(entity, new CharacterAnimatorReference
            {
                Animator = newGameObject.GetComponent<Animator>(),
                DeltaPosition = characterGameObjectPrefab.DeltaPosition
            });

            ecb.AddComponent(entity, new CharacterModelBones
            {
                HeadBoneTransform = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.HeadBoneName),
                ViewBoneTransform = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.ViewBoneName),
            });
        }

        foreach (var (transform, animatorReference, modelBones, animationState, localViewRotation) in SystemAPI
            .Query<RefRO<LocalTransform>, CharacterAnimatorReference, CharacterModelBones, RefRO<CharacterAnimationState>, RefRO<CharacterLocalViewRotation>>())
        {
            animatorReference.Animator.transform.position = transform.ValueRO.Position + animatorReference.DeltaPosition;
            animatorReference.Animator.transform.rotation = transform.ValueRO.Rotation;

            animatorReference.Animator.SetBool("IsWalking", animationState.ValueRO.IsWalking);

            modelBones.HeadBoneTransform.rotation = math.mul(transform.ValueRO.Rotation, localViewRotation.ValueRO.Value);
        }

        foreach (var (animatorReference, entity) in SystemAPI
            .Query<CharacterAnimatorReference>()
            .WithNone<CharacterGameObjectPrefab, LocalTransform>()
            .WithEntityAccess())
        {
            Object.Destroy(animatorReference.Animator.gameObject);
            ecb.RemoveComponent<CharacterAnimatorReference>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private Transform FindBoneByName(Transform parent, string boneName)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name == boneName)
                return child;
        }
        return null;
    }
}
