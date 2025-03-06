using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct WeaponAnimateSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        //state.RequireForUpdate<WeaponGameObjectPrefab>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        //Instanciate the visual prefab of weapon and add reference on him
        foreach (var (weaponViewPrefab, entity) in SystemAPI
            .Query<WeaponViewPrefab>()
            .WithNone<WeaponAnimatorReference>()
            .WithEntityAccess())
        {
            GameObject newGameObject = Object.Instantiate(weaponViewPrefab.GameObjectPrefab);
            ecb.AddComponent(entity, new WeaponAnimatorReference
            {
                Animator = newGameObject.GetComponent<Animator>(),
                Transform = newGameObject.transform
            });
        }

        //Attach to camera
        foreach (var (owner, animRef, entity) in SystemAPI
           .Query<RefRO<WeaponOwner>, WeaponAnimatorReference>()
           .WithEntityAccess())
        {
            if (state.EntityManager.HasComponent<CharacterModelBones>(owner.ValueRO.Value))
            {
                CharacterModelBones cameraTransform = state.EntityManager.GetComponentData<CharacterModelBones>(owner.ValueRO.Value);

                Vector3 cameraPos = cameraTransform.ViewBoneTransform.position;
                Vector3 cameraForward = cameraTransform.ViewBoneTransform.forward;

                animRef.Transform.position = cameraPos
                + cameraForward * 0.6f
                + animRef.Transform.right * 0.4f
                - animRef.Transform.up * 0.3f;

                animRef.Transform.rotation = cameraTransform.ViewBoneTransform.rotation;
            }
        }

        //FireAnim
        foreach (var (animRef, animState) in SystemAPI
           .Query<WeaponAnimatorReference, RefRW<WeaponAnimationState>>())
        {
            animRef.Animator.SetBool("IsFire", animState.ValueRO.IsFire);
        }

        //Clear Weapon View
        foreach (var (animatorRef, entity) in SystemAPI
            .Query<WeaponAnimatorReference>()
            .WithNone<WeaponViewPrefab, LocalTransform>()
            .WithEntityAccess())
        {
            Object.Destroy(animatorRef.Animator.gameObject);
            ecb.RemoveComponent<WeaponAnimatorReference>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

