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
                    //goRef.Value.GetComponent<Animator>().SetTrigger("Fire");
                    //goRef.Value.GetComponent<RangedWeaponSound>().Shoot();
                    data.ValueRW.state = RangedWeaponState.Idle;
                    break;
                case RangedWeaponState.Reload:
                    if (!alreadyReload)
                    {
                        //goRef.Value.GetComponent<RangedWeaponSound>().Reload();
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





