using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
//
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

            ecb.AddComponent(stuff, goRef.Instantiate(viewPrefabs, stuffDataRef));
        }

        //Attach to camera or drop
        foreach (var (ownerRO, stuffDataRO, transformRO, goRef, stuff) in SystemAPI
        .Query<RefRO<StuffDynamicData>, RefRO<StuffDatabaseAccess>, RefRO<LocalTransform>, StuffGameObjectRef>()
        //.WithAll<IsStuffOwnerUpdate>()
        .WithEntityAccess())
        {
            Entity owner = ownerRO.ValueRO.owner;

            //Si le stuff à un propriétaire, on l'attache au bone de la vue
            if (owner != Entity.Null)
            {
                GhostOwner ghostOwner = state.EntityManager.GetComponentData<GhostOwner>(owner);
                TeamSideType ownerSide = PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.NetworkId);
                GameObject stuffGo = goRef.GetGameObjectSide(ownerSide);

                if (stuffGo == null) continue;

                Transform stuffTransform = stuffGo.transform;

                //goRef.SetActive(ownerSide);

                if (state.EntityManager.HasComponent<CommonCharacterModelBonesTransform>(owner))
                {
                    Transform viewTransform = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(owner).WeaponSlotTransform;
                    if (stuffTransform.parent != viewTransform)
                    {
                        ref StuffCommonData stuffData = ref stuffDataRO.ValueRO.GetData(ref database);
                        stuffTransform.rotation = viewTransform.rotation;
                        stuffTransform.SetParent(viewTransform);
                        stuffTransform.localPosition = stuffData.GetStuffLocalOffsetView(ownerSide);

                        Animator animator = viewTransform.GetComponentInParent<Animator>();

                        if (animator != null)
                        {
                            animator.Rebind();
                        }
                    }
                }
            }
            //Si le stuff n'a pas de propriétaire et a un parent, on retire le parent
            else
            {
                if (goRef.GetGameObjectSide(TeamSideType.Corpo) != null)
                {
                    if (goRef.GetGameObjectSide(TeamSideType.Corpo).transform.parent != null)
                    {
                        goRef.SetParent(null);
                    }
                }

                if (goRef.GetGameObjectSide(TeamSideType.Natif) != null)
                {
                    if (goRef.GetGameObjectSide(TeamSideType.Natif).transform.parent != null)
                    {
                        goRef.SetParent(null);
                    }
                }
            }
        }

        //Stuff view follow Droped stuff
        foreach (var (inHandRefRO, transformRW, stuff) in SystemAPI
        .Query<RefRO<StuffEntityInHandRef>, RefRW<LocalTransform>>()
        .WithEntityAccess())
        {
            if (!state.EntityManager.HasComponent<StuffGameObjectRef>(inHandRefRO.ValueRO.Value)) continue;

            ref LocalTransform entityTransform = ref transformRW.ValueRW;
            StuffGameObjectRef goRef = state.EntityManager.GetComponentData<StuffGameObjectRef>(inHandRefRO.ValueRO.Value);
            Transform viewTransform = goRef.GetTransform();

            if (viewTransform == null) continue;

            if (viewTransform.parent == null)
            {
                goRef.SetTransform(entityTransform);

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
        foreach (var (goRef, dynDataRO, entity) in SystemAPI
        .Query<StuffGameObjectRef, RefRO<StuffDynamicData>>()
        .WithPresent<IsStuffInHand>()
        .WithEntityAccess())
        {
            if (dynDataRO.ValueRO.owner != Entity.Null)
            {
                GhostOwner ghostOwner = state.EntityManager.GetComponentData<GhostOwner>(dynDataRO.ValueRO.owner);
                TeamSideType ownerSide = PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.NetworkId);
                Transform stuffTransform = goRef.GetTransformSide(ownerSide);

                if (stuffTransform == null) continue;

                if (state.EntityManager.HasComponent<CommonCharacterModelBonesTransform>(dynDataRO.ValueRO.owner))
                {
                    CommonCharacterModelBonesTransform charaBones = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(dynDataRO.ValueRO.owner);
                    Transform viewTransform = charaBones.WeaponSlotTransform;

                    if (stuffTransform.parent != viewTransform)
                    {
                        stuffTransform.SetParent(viewTransform, false);

                        Animator animator = viewTransform.GetComponentInParent<Animator>();

                        if (animator != null)
                        {
                            animator.Rebind();
                        }
                    }
                }

                goRef.SwitchSetActiveSide(ownerSide, SystemAPI.IsComponentEnabled<IsStuffInHand>(entity));
            }
            else
            {
                goRef.SetActiveOne(true);
            }
        }

        //Clear Stuff GameObject
        foreach (var (goRef, entity) in SystemAPI
        .Query<StuffGameObjectRef>()
        .WithNone<LocalTransform>()
        .WithEntityAccess())
        {
            goRef.Destroy();
            ecb.RemoveComponent<StuffGameObjectRef>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
