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
            Debug.Log("Instanciate the visual prefab of weapon");

            GameObject newGameObject = Object.Instantiate(weaponGameObjectPrefab.GameObjectPrefab);
            ecb.AddComponent(entity, new WeaponAnimatorReference
            {
                Animator = newGameObject.GetComponent<Animator>(),
                Transform = newGameObject.transform
            });
        }

        //Attach to camera
        foreach (var (owner, stuffAnimRef, stuffEntity) in SystemAPI
           .Query<RefRO<Parent>, WeaponAnimatorReference>()
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
                        Debug.Log("Attach to camera");

                        LocalToWorld cameraTransform = state.EntityManager.GetComponentData<LocalToWorld>(child.Value);

                        Vector3 temp;
                        temp.x = cameraTransform.Position.x + 0.6f;
                        temp.y = cameraTransform.Position.y + 0.4f;
                        temp.z = cameraTransform.Position.z + 0.3f;
                        stuffAnimRef.Transform.position = temp;

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
            Debug.Log("Clear Weapon View");

            Object.Destroy(animatorReference.Animator.gameObject);
            ecb.RemoveComponent<WeaponAnimatorReference>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

