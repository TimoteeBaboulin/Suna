using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using RangedWeapon;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct RangedWeaponClientSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        //Attach to camera
        foreach (var (owner, transformRef, entity) in SystemAPI
           .Query<RefRO<StuffOwner>, RefRW<LocalTransform>>()
           .WithAll<IsStuffInHand>()
           .WithEntityAccess())
        {
            if (state.EntityManager.HasComponent<CharacterModelBones>(owner.ValueRO.Value))
            {
                CharacterModelBones cameraTransform = state.EntityManager.GetComponentData<CharacterModelBones>(owner.ValueRO.Value);

                Vector3 cameraPosBase = cameraTransform.ViewBoneTransform.position;
                Vector3 cameraForwardBase = cameraTransform.ViewBoneTransform.forward;

                float3 cameraPos = new(cameraPosBase);
                float3 cameraForward = new(cameraForwardBase);

                ref LocalTransform transform = ref transformRef.ValueRW;

                transform.Position = cameraPos
                + cameraForward * 0.6f
                + transform.Right() * 0.4f
                - transform.Up() * 0.3f;

                transform.Rotation = cameraTransform.ViewBoneTransform.rotation;
            }
        }

        //Active GameObject in hand
        foreach (var (animatorRef, entity) in SystemAPI
            .Query<StuffAnimatorRef>()
            .WithEntityAccess())
        {
            animatorRef.Animator.gameObject.SetActive(SystemAPI.IsComponentEnabled<IsStuffInHand>(entity));
        }

        //FireAnim
        foreach (var (animatorRef, dataRef) in SystemAPI
           .Query<StuffAnimatorRef, RefRO<DynamicData>>()
           .WithAll<IsStuffInHand>())
        {
            if (dataRef.ValueRO.state == _State.Shoot)
            {
                animatorRef.Animator.SetTrigger("Fire");
                //TODO :Je ne peux pas false la variable IsFire ici car c'est un GhostComponent
                // Si je retire le ghost, je ne peux plus la déclenché dans le shoot system car il est managé par le serveur
            }
        }

        //Clear Weapon View
        foreach (var (animatorRef, entity) in SystemAPI
            .Query<StuffAnimatorRef>()
            .WithNone<StuffPrefab, LocalTransform>()
            .WithEntityAccess())
        {
            Object.Destroy(animatorRef.Animator.gameObject);
            ecb.RemoveComponent<StuffAnimatorRef>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}




