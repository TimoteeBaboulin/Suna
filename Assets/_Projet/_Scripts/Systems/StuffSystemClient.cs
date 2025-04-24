//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;
//using UnityEngine;

//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
//partial struct StuffSystemClient : ISystem
//{
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<GameResourcesDatabase>();
//        state.RequireForUpdate<StuffOwner>();
//        state.RequireForUpdate<StuffDatabaseAccess>();

//        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithDisabled<StuffProcessPending>().Build(ref state);
//        state.RequireForUpdate(query);
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
//        GameResourcesDatabase grs = SystemAPI.GetSingleton<GameResourcesDatabase>();

//        //Instanciate GameObject and Attach to camera
//        foreach (var (owner, stuffDataRef, transform, entity) in SystemAPI
//            .Query<RefRO<StuffOwner>, RefRO<StuffDatabaseAccess>, RefRO<LocalToWorld>>()
//            .WithNone<StuffGameObjectRef>()
//            .WithDisabled<StuffProcessPending>()
//            .WithEntityAccess())
//        {
//            StuffGameObjectRef goRef = new StuffGameObjectRef();
//            ref StuffCommonData data = ref stuffDataRef.ValueRO.GetData(ref grs);

//            if (owner.ValueRO.Value != Entity.Null)
//            {
//                if (state.EntityManager.HasComponent<CommonCharacterModelBonesTransform>(owner.ValueRO.Value))
//                {
//                    CommonCharacterModelBonesTransform charaBones = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(owner.ValueRO.Value);
//                    Transform viewTransform = charaBones.WeaponSlotTransform;
//                    goRef.Value = Object.Instantiate(TempsStuffPrefabSingleton.Instance.listPrefabView[stuffDataRef.ValueRO.ID], viewTransform);
//                    //goRef.Value = Object.Instantiate(data.viewPrefab.Value, viewTransform);
//                    goRef.Value.transform.localPosition = data._stuffLocalOffsetView;
//                }
//            }
//            else
//            {
//                Vector3 pos = transform.ValueRO.Position;
//                quaternion rot = transform.ValueRO.Rotation;

//                goRef.Value = Object.Instantiate(TempsStuffPrefabSingleton.Instance.listPrefabView[stuffDataRef.ValueRO.ID], pos, rot);

//                //goRef.Value = Object.Instantiate(data.viewPrefab.Value, pos, rot);
//            }
//            Debug.Log(goRef.Value.name);
//            ecb.AddComponent(entity, goRef);
//        }

//        //Display stuff in hand
//        foreach (var (goRef, ownerRO, entity) in SystemAPI
//            .Query<StuffGameObjectRef, RefRO<StuffOwner>>()
//            .WithPresent<IsStuffInHand>()
//            //.WithNone<TemporaryOverrideGameObjectActive>() //TODO : Voir pour ça
//            .WithEntityAccess())
//        {
//            if (ownerRO.ValueRO.Value != Entity.Null)
//            {
//                if (state.EntityManager.HasComponent<CommonCharacterModelBonesTransform>(ownerRO.ValueRO.Value))
//                {
//                    CommonCharacterModelBonesTransform charaBones = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(ownerRO.ValueRO.Value);
//                    Transform viewTransform = charaBones.WeaponSlotTransform;

//                    if (goRef.Value.transform.parent != viewTransform)
//                    {
//                        goRef.Value.transform.SetParent(viewTransform, false);
//                    } 
//                }
//            }

//            goRef.Value.SetActive(SystemAPI.IsComponentEnabled<IsStuffInHand>(entity) || ownerRO.ValueRO.Value == Entity.Null);
//        }

//        //Render Thrown grenades
//        foreach (var (goRef, entityTransform, entity) in SystemAPI
//            .Query<StuffGameObjectRef, RefRO<LocalToWorld>>()
//            .WithPresent<ReleasedGrenade>()
//            .WithEntityAccess())
//        {
//            if (SystemAPI.IsComponentEnabled<ReleasedGrenade>(entity))
//            {
//                goRef.Value.SetActive(goRef.Value != null);
//                goRef.Value.transform.SetParent(null, true);
//                goRef.Value.transform.position = entityTransform.ValueRO.Position;
//                goRef.Value.transform.rotation = entityTransform.ValueRO.Rotation;
//                goRef.Value.transform.localScale = Vector3.one;
//            }
//        }

//        //Clear Stuff GameObject
//        foreach (var (goRef, entity) in SystemAPI
//            .Query<StuffGameObjectRef>()
//            .WithNone<LocalTransform>()
//            .WithEntityAccess())
//        {
//            Object.Destroy(goRef.Value);
//            ecb.RemoveComponent<StuffGameObjectRef>(entity);
//        }

//        ecb.Playback(state.EntityManager);
//        ecb.Dispose();
//    }
//}

//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
//public partial class StuffSystemClient : SystemBase
//{
//    private EntityQuery _query;

//    protected override void OnCreate()
//    {
//        RequireForUpdate<GameResourcesDatabase>();
//        RequireForUpdate<StuffOwner>();
//        RequireForUpdate<StuffDatabaseAccess>();

//        _query = GetEntityQuery(new EntityQueryDesc
//        {
//            None = new ComponentType[] { typeof(StuffProcessPending) }
//        });
//        RequireForUpdate(_query);
//    }

//    protected override void OnUpdate()
//    {
//        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
//        GameResourcesDatabase grs = SystemAPI.GetSingleton<GameResourcesDatabase>();

//        foreach (var (owner, stuffDataRef, transform, entity) in SystemAPI
//                    .Query<RefRO<StuffOwner>, RefRO<StuffDatabaseAccess>, RefRO<LocalToWorld>>()
//                    .WithNone<StuffGameObjectRef>()
//                    .WithDisabled<StuffProcessPending>()
//                    .WithEntityAccess())
//        {
//            StuffGameObjectRef goRef = new StuffGameObjectRef();
//            ref StuffCommonData data = ref stuffDataRef.ValueRO.GetData(ref grs);

//            if (owner.ValueRO.Value != Entity.Null)
//            {
//                if (EntityManager.HasComponent<CommonCharacterModelBonesTransform>(owner.ValueRO.Value))
//                {
//                    CommonCharacterModelBonesTransform charaBones = EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(owner.ValueRO.Value);
//                    Transform viewTransform = charaBones.WeaponSlotTransform;
//                    goRef.Value = Object.Instantiate(data.viewPrefab.Value, viewTransform);
//                    goRef.Value.transform.localPosition = data._stuffLocalOffsetView;
//                }
//            }
//            else
//            {
//                Vector3 pos = transform.ValueRO.Position;
//                quaternion rot = transform.ValueRO.Rotation;

//                goRef.Value = Object.Instantiate(data.viewPrefab.Value, pos, rot);
//            }

//            ecb.AddComponent(entity, goRef);
//        }

//        ecb.Playback(EntityManager);
//        ecb.Dispose();
//    }
//}
