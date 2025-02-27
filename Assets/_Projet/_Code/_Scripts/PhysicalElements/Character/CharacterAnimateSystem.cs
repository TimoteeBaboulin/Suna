using Unity.Collections;
using Unity.Entities;
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
        }

        foreach (var (transform, animatorReference, animationState) in SystemAPI
            .Query<LocalTransform, CharacterAnimatorReference, RefRO<CharacterAnimationState>>())
        {
            animatorReference.Animator.transform.position = transform.Position + animatorReference.DeltaPosition;
            animatorReference.Animator.transform.rotation = transform.Rotation;

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
}
