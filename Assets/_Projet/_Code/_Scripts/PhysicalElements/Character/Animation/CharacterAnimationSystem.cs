using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

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

            if (state.World.IsServer() && newGameObject.TryGetComponent(out CharacterModelScript characterModelScript))
            {
                characterModelScript.MeshRenderer.enabled = false;
            }

            ecb.AddComponent(entity, new CharacterAnimatorReference
            {
                Animator = newGameObject.GetComponent<Animator>(),
                CharacterModel = newGameObject.GetComponent<CharacterModelScript>(),
                DeltaPosition = characterGameObjectPrefab.DeltaPosition
            });

            ecb.AddComponent(entity, new CharacterModelBones
            {
                HeadBoneTransform = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.HeadBoneName),
                ViewBoneTransform = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.ViewBoneName),
                ArmLeftBoneTransform0 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.ArmLeftBoneName0),
                ArmLeftBoneTransform1 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.ArmLeftBoneName1),
                ArmLeftBoneTransform2 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.ArmLeftBoneName2),
                ArmRightBoneTransform0 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.ArmRightBoneName0),
                ArmRightBoneTransform1 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.ArmRightBoneName1),
                ArmRightBoneTransform2 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.ArmRightBoneName2),
                ThoraxBoneTransform = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.ThoraxBoneName),
                StomachBoneTransform0 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.StomachBoneName0),
                StomachBoneTransform1 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.StomachBoneName1),
                LegLeftBoneTransform0 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.LegLeftBoneName0),
                LegLeftBoneTransform1 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.LegLeftBoneName1),
                LegLeftBoneTransform2 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.LegLeftBoneName2),
                LegRightBoneTransform0 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.LegRightBoneName0),
                LegRightBoneTransform1 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.LegRightBoneName1),
                LegRightBoneTransform2 = FindBoneByName(newGameObject.transform, characterGameObjectPrefab.LegRightBoneName2),
            });
        }

        foreach (var (transform, animatorReference, modelBones, animationState, localViewRotation) in SystemAPI
            .Query<RefRO<LocalTransform>, CharacterAnimatorReference, CharacterModelBones, RefRO<CharacterAnimationState>, RefRO<CharacterLocalViewRotation>>())
        {
            animatorReference.Animator.transform.position = transform.ValueRO.Position + animatorReference.DeltaPosition;
            animatorReference.Animator.transform.rotation = transform.ValueRO.Rotation;

            animatorReference.Animator.SetBool("IsWalking", animationState.ValueRO.IsWalking);

            animatorReference.CharacterModel.NewHeadRotation = math.mul(transform.ValueRO.Rotation, localViewRotation.ValueRO.ViewRotation);
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
