using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct RangedWeaponViewSystem : ISystem
{
    bool alreadyReload;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffGameObjectRef>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        //Play Anim (Temp)
        foreach (var (goRef, data, entity) in SystemAPI
            .Query<StuffGameObjectRef, RefRW<RangedWeaponDynamicData>>()
            .WithAll<IsStuffInHand>()
            .WithEntityAccess())
        {
            alreadyReload = data.ValueRO.state != RangedWeaponState.Reload ? false : alreadyReload;
            switch (data.ValueRO.state)
            {
                case RangedWeaponState.Idle:
                    break;
                case RangedWeaponState.Shoot:
#if !UNITY_SERVER
                    if (goRef.Value.TryGetComponent(out RangedWeaponSound rws))
                    {
                        rws.Shoot();
                    }
#endif
                    data.ValueRW.state = RangedWeaponState.Idle;
                    break;
                case RangedWeaponState.Reload:
                    if (!alreadyReload)
                    {
#if !UNITY_SERVER
                        if (goRef.Value.TryGetComponent(out rws))
                        {
                            rws.Reload();
                        }
#endif

                        alreadyReload = true;
                    }

                    break;
                case RangedWeaponState.Droped:
                    break;
                default:
                    break;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}





