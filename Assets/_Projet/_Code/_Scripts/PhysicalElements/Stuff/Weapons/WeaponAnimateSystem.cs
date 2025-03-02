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
        foreach (var (weaponGameObjectPrefab, entity) in SystemAPI
            .Query<WeaponGameObjectPrefab>()
            .WithNone<WeaponAnimatorReference>()
            .WithEntityAccess())
        {
            GameObject newGameObject = Object.Instantiate(weaponGameObjectPrefab.GameObjectPrefab);
            ecb.AddComponent(entity, new WeaponAnimatorReference
            {
                Animator = newGameObject.GetComponent<Animator>(),
                Transform = newGameObject.transform
            });
        }

        //Attach to camera
        foreach (var (owner, stuffAnimRef, stuffEntity) in SystemAPI
           .Query<RefRO<WeaponOwner>, WeaponAnimatorReference>()
           .WithAll<WeaponGameObjectPrefab>()
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

                        stuffAnimRef.Transform.position = cameraPos
                        + cameraForward * 0.6f
                        + stuffAnimRef.Transform.right * 0.4f
                        - stuffAnimRef.Transform.up * 0.3f;

                        stuffAnimRef.Transform.rotation = cameraTransform.Rotation;
                    }
                }
            }
        }

        //Clear Weapon View
        foreach (var (animatorReference, entity) in SystemAPI
            .Query<WeaponAnimatorReference>()
            .WithNone<WeaponGameObjectPrefab, LocalTransform>()
            .WithEntityAccess())
        {
            Object.Destroy(animatorReference.Animator.gameObject);
            ecb.RemoveComponent<WeaponAnimatorReference>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

