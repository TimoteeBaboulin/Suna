using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct StuffSystems : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        //Instanciate GameObject and Attach to camera
        foreach (var (owner, prefabRef, stuffData, entity) in SystemAPI
            .Query<RefRO<StuffOwner>, StuffGameObjectPrefab, StuffCommonData>()
            .WithNone<StuffGameObjectRef>()
            .WithEntityAccess())
        {
            if (state.EntityManager.HasComponent<CharacterModelBones>(owner.ValueRO.Value))
            {
                CharacterModelBones charaBones = state.EntityManager.GetComponentData<CharacterModelBones>(owner.ValueRO.Value);
                Transform viewTransform = charaBones.ViewBoneTransform;

                StuffGameObjectRef goRef = new StuffGameObjectRef
                { 
                    Value = Object.Instantiate(prefabRef.Value, viewTransform) 
                };

                goRef.Value.transform.localPosition = stuffData._stuffLocalOffsetView;

                ecb.AddComponent(entity, goRef);
            }
        }

        //Display stuff in hand
        foreach (var (goRef, entity) in SystemAPI
            .Query<StuffGameObjectRef>()
            .WithPresent<IsStuffInHand>()
            .WithEntityAccess())
        {
            goRef.Value.SetActive(SystemAPI.IsComponentEnabled<IsStuffInHand>(entity));
        }

        //Clear Stuff GameObject
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
