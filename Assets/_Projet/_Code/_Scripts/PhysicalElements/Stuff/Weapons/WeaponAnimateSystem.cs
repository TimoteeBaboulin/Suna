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
            .WithNone<WeaponAnimatorRef>()
            .WithAll<ActiveWeaponTag>()
            .WithEntityAccess())
        {
            Debug.Log("HEHO !");

            GameObject newGameObject = Object.Instantiate(weaponViewPrefab.GameObjectPrefab);
            ecb.AddComponent(entity, new WeaponAnimatorRef
            {
                Animator = newGameObject.GetComponent<Animator>(),
                Transform = newGameObject.transform
            });
        }

        //Attach to camera
        foreach (var (owner, animRef, entity) in SystemAPI
           .Query<RefRO<WeaponOwner>, WeaponAnimatorRef>()
           .WithAll<ActiveWeaponTag>()
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
        foreach (var (animatorRef, animStateRef) in SystemAPI
           .Query<WeaponAnimatorRef, RefRW<WeaponAnimationState>>()
           .WithAll<ActiveWeaponTag>())
        {
            ref WeaponAnimationState animState = ref animStateRef.ValueRW;
            if (animState.IsFire)
            {
                animatorRef.Animator.SetTrigger("Fire");
                //TODO :Je ne peux pas false la variable IsFire ici car c'est un GhostComponent
                // Si je retire le ghost, je ne peux plus la déclenché dans le shoot system car il est managé par le serveur
            }
        }

        //Clear Weapon View
        foreach (var (animatorRef, entity) in SystemAPI
            .Query<WeaponAnimatorRef>()
            .WithNone<WeaponViewPrefab, LocalTransform>()
            .WithEntityAccess())
        {
            Object.Destroy(animatorRef.Animator.gameObject);
            ecb.RemoveComponent<WeaponAnimatorRef>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

