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
                    GhostOwner ghostOwner = state.EntityManager.GetComponentData<GhostOwner>(dynDataRO.ValueRO.owner);
                    TeamSideType ownerSide = PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.NetworkId);
                    ref StuffCommonData stuffData = ref stuffDataRO.ValueRO.GetData(ref database);

                    goRef.SetParent(ownerView);
                    goRef.SetLocalTransform(stuffData.GetStuffLocalOffsetView(ownerSide), ownerView.rotation);

                    Animator animator = ownerView.GetComponentInParent<Animator>();
                    if (animator != null) animator.Rebind();
                }
            }
            else
            {
                goRef.SetParent(null);
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
                GhostOwner ghostOwner = state.EntityManager.GetComponentData<GhostOwner>(dynDataRO.ValueRO.owner);
                TeamSideType ownerSide = PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.NetworkId);

                if (SystemAPI.IsComponentEnabled<IsStuffInHand>(entity))
                {
                    goRef.SwitchSetActiveSide(ownerSide, true);
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
