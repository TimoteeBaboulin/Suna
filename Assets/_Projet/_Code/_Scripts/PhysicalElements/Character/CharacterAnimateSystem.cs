using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct CharacterAnimateSystem : ISystem
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
                HeadBoneTransform = FindBoneByName(newGameObject.transform, "B-head")
            });
        }

        foreach (var (transform, animatorReference, modelBones, animationState, viewEntity) in SystemAPI
            .Query<LocalTransform, CharacterAnimatorReference, CharacterModelBones, RefRO<CharacterAnimationState>, RefRO<CharacterViewEntityComponent>>())
        {
            animatorReference.Animator.transform.position = transform.Position + animatorReference.DeltaPosition;
            animatorReference.Animator.transform.rotation = transform.Rotation;

            RefRO<LocalTransform> viewTransform = SystemAPI.GetComponentRO<LocalTransform>(viewEntity.ValueRO.Value);
            modelBones.HeadBoneTransform.rotation = math.mul(transform.Rotation, viewTransform.ValueRO.Rotation);

            animatorReference.Animator.SetBool("IsWalking", animationState.ValueRO.IsWalking);
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
