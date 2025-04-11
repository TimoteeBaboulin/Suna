using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct StuffSystemClient : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResourcesDatabase>();
        state.RequireForUpdate<StuffOwner>();
        state.RequireForUpdate<StuffDatabaseAccess>();

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithDisabled<StuffProcessPending>().Build(ref state);
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        GameResourcesDatabase database = SystemAPI.GetSingleton<GameResourcesDatabase>();

        //Instanciate GameObject
        foreach (var (ownerRO, stuffDataRef, transform, stuff) in SystemAPI
        .Query<RefRO<StuffOwner>, RefRO<StuffDatabaseAccess>, RefRO<LocalToWorld>>()
        .WithNone<StuffGameObjectRef>()
        .WithDisabled<StuffProcessPending>()
        .WithEntityAccess())
        {
            StuffGameObjectRef goRef = new StuffGameObjectRef();
            Entity owner = ownerRO.ValueRO.Value;
            ref StuffCommonData data = ref stuffDataRef.ValueRO.GetData(ref database);

            goRef.Value = Object.Instantiate(TempsStuffPrefabSingleton.Instance.listPrefabView[stuffDataRef.ValueRO.ID]);
            ecb.AddComponent(stuff, goRef);
        }

        //Attach to camera or drop
        foreach (var (ownerRO, stuffDataRO, transformRO, goRef, stuff) in SystemAPI
        .Query<RefRO<StuffOwner>, RefRO<StuffDatabaseAccess>, RefRO<LocalToWorld>, StuffGameObjectRef>()
        .WithAll<IsStuffViewChangeParent>()
        .WithEntityAccess())
        {
            Entity owner = ownerRO.ValueRO.Value;
            Transform stuffTrasform = goRef.Value.transform;

            //Si le stuff à un propriétaire, on l'attache au bone de la vue
            if (owner != Entity.Null)
            {
                if (state.EntityManager.HasComponent<CommonCharacterModelBonesTransform>(owner))
                {
                    Transform viewTransform = state.EntityManager.GetComponentData<CommonCharacterModelBonesTransform>(owner).WeaponSlotTransform;
                    ref StuffCommonData stuffData = ref stuffDataRO.ValueRO.GetData(ref database);

                    stuffTrasform.rotation = viewTransform.rotation;
                    stuffTrasform.SetParent(viewTransform);
                    stuffTrasform.localPosition = stuffData._stuffLocalOffsetView;
                }
            }
            //Si le stuff n'a pas de propriétaire, on le drop au sol
            else
            {
                stuffTrasform.localPosition = default;
                stuffTrasform.SetParent(null);

                Vector3 pos = transformRO.ValueRO.Position;
                quaternion rot = transformRO.ValueRO.Rotation;
                stuffTrasform.position = pos;
                stuffTrasform.rotation = rot;
            }

            state.EntityManager.SetComponentEnabled<IsStuffViewChangeParent>(stuff, false);
        }

        //Display stuff in hand
        foreach (var (goRef, ownerRO, entity) in SystemAPI
        .Query<StuffGameObjectRef, RefRO<StuffOwner>>()
        .WithPresent<IsStuffInHand>()
        .WithEntityAccess())
        {
            goRef.Value.SetActive(SystemAPI.IsComponentEnabled<IsStuffInHand>(entity) || ownerRO.ValueRO.Value == Entity.Null);
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
