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
            if (state.EntityManager.HasBuffer<Child>(owner.ValueRO.Value))
            {
                DynamicBuffer<Child> childBuffer = state.EntityManager.GetBuffer<Child>(owner.ValueRO.Value);
                foreach (Child child in childBuffer)
                {
                    if (state.EntityManager.HasComponent<MainEntityCameraTag>(child.Value))
                    {
                        LocalToWorld cameraTransform = state.EntityManager.GetComponentData<LocalToWorld>(child.Value);
                        Vector3 cameraPos = cameraTransform.Position;
                        Vector3 cameraForward = cameraTransform.Forward;

                        animRef.Transform.position = cameraPos
                        + cameraForward * 0.6f
                        + animRef.Transform.right * 0.4f
                        - animRef.Transform.up * 0.3f;

                        animRef.Transform.rotation = cameraTransform.Rotation;
                    }
                }
            }
        }

        //FireAnim
        foreach (var (animRef, animState) in SystemAPI
           .Query<WeaponAnimatorReference, RefRO<WeaponAnimationState>>())
        {
            if (Input.GetMouseButtonDown(0))
            {
                animRef.Animator.SetTrigger("Fire");
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                animRef.Animator.SetTrigger("Reload");
            }
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

