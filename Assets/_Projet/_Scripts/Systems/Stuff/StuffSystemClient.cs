using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct StuffSystemClient : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResourcesDatabase>();
        state.RequireForUpdate<StuffDynamicData>();
        state.RequireForUpdate<StuffDatabaseAccess>();

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithDisabled<StuffProcessPending>().Build(ref state);
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        GameResourcesDatabase database = SystemAPI.GetSingleton<GameResourcesDatabase>();

        //Instanciate GameObject
        foreach (var (dynDataRO, stuffDataRef, transform, stuff) in SystemAPI
        .Query<RefRO<StuffDynamicData>, RefRO<StuffDatabaseAccess>, RefRO<LocalToWorld>>()
        .WithNone<StuffGameObjectRef>()
        .WithDisabled<StuffProcessPending>()
        .WithEntityAccess())
        {
            StuffGameObjectRef goRef = new StuffGameObjectRef();
            ref StuffCommonData data = ref stuffDataRef.ValueRO.GetData(ref database);
            var singletonEntity = SystemAPI.GetSingletonEntity<GameResourcesDatabase>();
            var viewPrefabs = state.EntityManager.GetComponentObject<GameResourcesViewPrefabs>(singletonEntity);

            goRef.Value = Object.Instantiate(viewPrefabs.List[stuffDataRef.ValueRO.ID]);
            ecb.AddComponent(stuff, goRef);
        }

        //Attach to camera or drop
        foreach (var (ownerRO, stuffDataRO, transformRO, goRef, stuff) in SystemAPI
        .Query<RefRO<StuffDynamicData>, RefRO<StuffDatabaseAccess>, RefRO<LocalTransform>, StuffGameObjectRef>()
        //.WithAll<IsStuffOwnerUpdate>()
        .WithEntityAccess())
        {
            Entity owner = ownerRO.ValueRO.owner;
            Transform stuffTransform = goRef.Value.transform;

            //Si le stuff à un propriétaire, on l'attache au bone de la vue
            if (owner != Entity.Null)
            {
                if (state.EntityManager.HasComponent<CommonCharacterModelBonesTransform>(owner))
                {
                    Transform viewTransform = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(owner).WeaponSlotTransform;
                    if (stuffTransform.parent != viewTransform)
                    {
                        ref StuffCommonData stuffData = ref stuffDataRO.ValueRO.GetData(ref database);
                        stuffTransform.rotation = viewTransform.rotation;
                        stuffTransform.SetParent(viewTransform);
                        stuffTransform.localPosition = stuffData._stuffLocalOffsetView;
                    }
                }
            }
            //Si le stuff n'a pas de propriétaire et a un parent, on retire le parent
            else if (stuffTransform.parent != null)
            {
                stuffTransform.SetParent(null);
            }
        }

        //Stuff view follow Droped stuff
        foreach (var (inHandRefRO, transformRW, stuff) in SystemAPI
        .Query<RefRO<StuffEntityInHandRef>, RefRW<LocalTransform>>()
        .WithEntityAccess())
        {
            if (!state.EntityManager.HasComponent<StuffGameObjectRef>(inHandRefRO.ValueRO.Value)) continue;

            ref LocalTransform entityTransform = ref transformRW.ValueRW;
            Transform viewTransform = state.EntityManager.GetComponentData<StuffGameObjectRef>(inHandRefRO.ValueRO.Value).Value.transform;

            if (viewTransform.parent == null)
            {
                viewTransform.position = entityTransform.Position;
                viewTransform.rotation = entityTransform.Rotation;

                if (state.EntityManager.HasComponent<ReleasedGrenade>(inHandRefRO.ValueRO.Value))
                {
                    if (state.EntityManager.IsComponentEnabled<ReleasedGrenade>(inHandRefRO.ValueRO.Value))
                    {
                        viewTransform.localScale = Vector3.one * .8f;
                    }
                }
            }
        }

        //Display stuff
        foreach (var (goRef, ownerRO, entity) in SystemAPI
        .Query<StuffGameObjectRef, RefRO<StuffDynamicData>>()
        .WithPresent<IsStuffInHand>()
        .WithEntityAccess())
        {
            if (ownerRO.ValueRO.owner != Entity.Null)
            {
                if (state.EntityManager.HasComponent<CommonCharacterModelBonesTransform>(ownerRO.ValueRO.owner))
                {
                    CommonCharacterModelBonesTransform charaBones = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(ownerRO.ValueRO.owner);
                    Transform viewTransform = charaBones.WeaponSlotTransform;

                    if (goRef.Value.transform.parent != viewTransform)
                    {
                        goRef.Value.transform.SetParent(viewTransform, false);
                    }
                }
            }

            goRef.Value.SetActive(SystemAPI.IsComponentEnabled<IsStuffInHand>(entity) || ownerRO.ValueRO.owner == Entity.Null);
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
