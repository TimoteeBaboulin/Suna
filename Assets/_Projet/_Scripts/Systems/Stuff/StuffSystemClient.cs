using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
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
            var singletonEntity = SystemAPI.GetSingletonEntity<GameResourcesDatabase>();
            var viewPrefabs = state.EntityManager.GetComponentObject<GameResourcesViewPrefabs>(singletonEntity);

            goRef.Instantiate(viewPrefabs, stuffDataRef);

            ecb.AddComponent(stuff, goRef);
        }

        //Attach to character view or not
        foreach (var (dynDataRO, stuffDataRO, transformRO, goRef, stuff) in SystemAPI
        .Query<RefRO<StuffDynamicData>, RefRO<StuffDatabaseAccess>, RefRO<LocalTransform>, StuffGameObjectRef>()
        .WithEntityAccess())
        {
            if (dynDataRO.ValueRO.owner != Entity.Null)
            {
                Transform ownerView = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(dynDataRO.ValueRO.owner).WeaponSlotTransform;

                if (ownerView != goRef.GetOneTransform().parent)
                {
                    var ownerGhostOwner = state.EntityManager.GetComponentData<GhostOwner>(dynDataRO.ValueRO.owner);
                    TeamSideType ownerSide = PlayerHelpers.GetPlayerInTeam(ownerGhostOwner.NetworkId);
                    if (ownerSide == TeamSideType.Neutre) continue;

                    ref StuffCommonData stuffData = ref stuffDataRO.ValueRO.GetData(ref database);
                    goRef.SetParent(ownerView);
                    goRef.SetLayer(dynDataRO.ValueRO.owner, state.EntityManager);

                    goRef.SetLocalTransform(stuffData.GetStuffLocalOffsetView(ownerSide), ownerView.rotation);

                    Animator animator = ownerView.GetComponentInParent<Animator>();
                    if (animator != null) animator.Rebind();
                }
            }
            else
            {
                goRef.SetParent(null);
                goRef.SetLayer(dynDataRO.ValueRO.owner, state.EntityManager);
            }
        }

        //View follow Droped stuff
        foreach (var (inHandRefRO, transformRO, stuff) in SystemAPI
        .Query<RefRO<StuffEntityInHandRef>, RefRO<LocalTransform>>()
        .WithEntityAccess())
        {
            if (state.EntityManager.HasComponent<StuffGameObjectRef>(inHandRefRO.ValueRO.Value))
            {
                StuffGameObjectRef goRef = state.EntityManager.GetComponentData<StuffGameObjectRef>(inHandRefRO.ValueRO.Value);
                Transform goTransform = goRef.GetOneTransform();

                if (goTransform != null)
                {
                    if (goTransform.parent == null)
                    {
                        goRef.SetTransform(transformRO.ValueRO);

                        //////////////GRENADES//////////////
                        if (state.EntityManager.HasComponent<ReleasedGrenade>(inHandRefRO.ValueRO.Value))
                        {
                            if (state.EntityManager.IsComponentEnabled<ReleasedGrenade>(inHandRefRO.ValueRO.Value))
                            {
                                goRef.SetLocalScale(.8f);
                            }
                        }
                    }
                }
            }
        }

        //Active or Unactive GameObject
        foreach (var (goRef, dynDataRO, entity) in SystemAPI
        .Query<StuffGameObjectRef, RefRO<StuffDynamicData>>()
        .WithPresent<IsStuffInHand>()
        .WithEntityAccess())
        {
            if (dynDataRO.ValueRO.owner != Entity.Null)
            {

                if (SystemAPI.IsComponentEnabled<IsStuffInHand>(entity))
                {
                    var ownerGhostOwner = state.EntityManager.GetComponentData<GhostOwner>(dynDataRO.ValueRO.owner);
                    TeamSideType ownerSide = PlayerHelpers.GetPlayerInTeam(ownerGhostOwner.NetworkId);

                    if (state.EntityManager.HasComponent<CharacterIsDifusing>(dynDataRO.ValueRO.owner))
                    {
                        bool isDifusing = state.EntityManager.IsComponentEnabled<CharacterIsDifusing>(dynDataRO.ValueRO.owner);
                        goRef.SwitchSetActiveSide(ownerSide, !isDifusing);
                    }
                }
                else
                {
                    goRef.SetActive(false);
                }
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

//using Unity.Collections;
//using Unity.Entities;
//using Unity.Transforms;
//using UnityEngine;
////
//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
//partial struct StuffSystemClient : ISystem
//{
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<GameResourcesDatabase>();
//        state.RequireForUpdate<StuffDynamicData>();
//        state.RequireForUpdate<StuffDatabaseAccess>();

//        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithDisabled<StuffProcessPending>().Build(ref state);
//        state.RequireForUpdate(query);
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
//        GameResourcesDatabase database = SystemAPI.GetSingleton<GameResourcesDatabase>();

//        //Instanciate GameObject
//        foreach (var (dynDataRO, stuffDataRef, transform, stuff) in SystemAPI
//        .Query<RefRO<StuffDynamicData>, RefRO<StuffDatabaseAccess>, RefRO<LocalToWorld>>()
//        .WithNone<StuffGameObjectRef>()
//        .WithDisabled<StuffProcessPending>()
//        .WithEntityAccess())
//        {
//            StuffGameObjectRef goRef = new StuffGameObjectRef();
//            ref StuffCommonData data = ref stuffDataRef.ValueRO.GetData(ref database);
//            var singletonEntity = SystemAPI.GetSingletonEntity<GameResourcesDatabase>();
//            var viewPrefabs = state.EntityManager.GetComponentObject<GameResourcesViewPrefabs>(singletonEntity);

//            goRef.Value = Object.Instantiate(viewPrefabs.List[stuffDataRef.ValueRO.ID]);
//            goRef.Value.name = viewPrefabs.List[stuffDataRef.ValueRO.ID].name;
//            ecb.AddComponent(stuff, goRef);
//        }

//        //Attach to camera or drop
//        foreach (var (ownerRO, stuffDataRO, transformRO, goRef, stuff) in SystemAPI
//        .Query<RefRO<StuffDynamicData>, RefRO<StuffDatabaseAccess>, RefRO<LocalTransform>, StuffGameObjectRef>()
//        //.WithAll<IsStuffOwnerUpdate>()
//        .WithEntityAccess())
//        {
//            Entity owner = ownerRO.ValueRO.owner;
//            Transform stuffTransform = goRef.Value.transform;

//            //Si le stuff à un propriétaire, on l'attache au bone de la vue
//            if (owner != Entity.Null)
//            {
//                if (state.EntityManager.HasComponent<CommonCharacterModelBonesTransform>(owner))
//                {
//                    Transform viewTransform = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(owner).WeaponSlotTransform;
//                    if (stuffTransform.parent != viewTransform)
//                    {
//                        ref StuffCommonData stuffData = ref stuffDataRO.ValueRO.GetData(ref database);
//                        stuffTransform.rotation = viewTransform.rotation;
//                        stuffTransform.SetParent(viewTransform);
//                        stuffTransform.localPosition = stuffData._stuffLocalOffsetView;

//                        Animator animator = viewTransform.GetComponentInParent<Animator>();

//                        if (animator != null)
//                        {
//                            animator.Rebind();
//                        }
//                    }
//                }
//            }
//            //Si le stuff n'a pas de propriétaire et a un parent, on retire le parent
//            else if (stuffTransform.parent != null)
//            {
//                stuffTransform.SetParent(null);
//            }
//        }

//        //Stuff view follow Droped stuff
//        foreach (var (inHandRefRO, transformRW, stuff) in SystemAPI
//        .Query<RefRO<StuffEntityInHandRef>, RefRW<LocalTransform>>()
//        .WithEntityAccess())
//        {
//            if (!state.EntityManager.HasComponent<StuffGameObjectRef>(inHandRefRO.ValueRO.Value)) continue;

//            ref LocalTransform entityTransform = ref transformRW.ValueRW;
//            Transform viewTransform = state.EntityManager.GetComponentData<StuffGameObjectRef>(inHandRefRO.ValueRO.Value).Value.transform;

//            if (viewTransform.parent == null)
//            {
//                viewTransform.position = entityTransform.Position;
//                viewTransform.rotation = entityTransform.Rotation;

//                if (state.EntityManager.HasComponent<ReleasedGrenade>(inHandRefRO.ValueRO.Value))
//                {
//                    if (state.EntityManager.IsComponentEnabled<ReleasedGrenade>(inHandRefRO.ValueRO.Value))
//                    {
//                        viewTransform.localScale = Vector3.one * .8f;
//                    }
//                }
//            }
//        }

//        //Display stuff
//        foreach (var (goRef, dynDataRO, entity) in SystemAPI
//        .Query<StuffGameObjectRef, RefRO<StuffDynamicData>>()
//        .WithPresent<IsStuffInHand>()
//        .WithEntityAccess())
//        {
//            if (dynDataRO.ValueRO.owner != Entity.Null)
//            {
//                if (state.EntityManager.HasComponent<CommonCharacterModelBonesTransform>(dynDataRO.ValueRO.owner))
//                {
//                    CommonCharacterModelBonesTransform charaBones = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(dynDataRO.ValueRO.owner);
//                    Transform viewTransform = charaBones.WeaponSlotTransform;

//                    if (goRef.Value.transform.parent != viewTransform)
//                    {
//                        goRef.Value.transform.SetParent(viewTransform, false);

//                        Animator animator = viewTransform.GetComponentInParent<Animator>();

//                        if (animator != null)
//                        {
//                            animator.Rebind();
//                        }
//                    }
//                }
//            }

//            goRef.Value.SetActive(SystemAPI.IsComponentEnabled<IsStuffInHand>(entity) || dynDataRO.ValueRO.owner == Entity.Null);
//        }

//        //Clear Stuff GameObject
//        foreach (var (goRef, entity) in SystemAPI
//        .Query<StuffGameObjectRef>()
//        .WithNone<LocalTransform>()
//        .WithEntityAccess())
//        {
//            Object.Destroy(goRef.Value);
//            ecb.RemoveComponent<StuffGameObjectRef>(entity);
//        }

//        ecb.Playback(state.EntityManager);
//        ecb.Dispose();
//    }
//}
