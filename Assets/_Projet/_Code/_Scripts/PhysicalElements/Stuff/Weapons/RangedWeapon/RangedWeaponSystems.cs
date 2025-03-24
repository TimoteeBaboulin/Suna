using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

using RangedWeapon;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct RangedWeaponViewSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        //Instanciate GameObject and Attach to camera
        foreach (var (owner, prefabRef, stuffData, entity) in SystemAPI
            .Query<RefRO<StuffOwner>, StuffGameObjectPrefab, StuffCommonData> ()
            .WithNone<StuffGameObjectRef>()
            .WithEntityAccess())
        {
            if (state.EntityManager.HasComponent<CharacterModelBones>(owner.ValueRO.Value))
            {
                CharacterModelBones charaBones = state.EntityManager.GetComponentData<CharacterModelBones>(owner.ValueRO.Value);
                Transform viewTransform = charaBones.ViewBoneTransform;

                StuffGameObjectRef goRef = new StuffGameObjectRef{ Value = Object.Instantiate(prefabRef.Value, viewTransform) };
                goRef.Value.transform.localPosition = stuffData._stuffLocalOffsetView;

                //goRef.Value.GetComponent<Animator>().contr;


                ecb.AddComponent(entity, goRef);
            }
        }

        //Active GameObject in hand
        foreach (var (goRef, entity) in SystemAPI
            .Query<StuffGameObjectRef>()
            .WithEntityAccess())
        {
            goRef.Value.SetActive(SystemAPI.IsComponentEnabled<IsStuffInHand>(entity));
        }

        //FireAnim
        foreach (var (goRef, dataRef) in SystemAPI
           .Query<StuffGameObjectRef, RefRO<DynamicData>>()
           .WithAll<IsStuffInHand>())
        {
            if (dataRef.ValueRO.state == _State.Shoot)
            {
                goRef.Value.GetComponent<Animator>().SetTrigger("Fire");
            }
        }

        //Clear Weapon View
        foreach (var (goRef, entity) in SystemAPI
            .Query<StuffGameObjectRef>()
            .WithNone<LocalTransform>()
            .WithEntityAccess())
        {
            Object.Destroy(goRef.Value);
            ecb.RemoveComponent<StuffGameObjectRef>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}




