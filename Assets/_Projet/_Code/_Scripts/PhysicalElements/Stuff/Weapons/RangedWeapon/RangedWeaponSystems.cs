using Unity.Collections;
using Unity.Entities;
using UnityEngine;

using RangedWeapon;
using UnityEngine.VFX;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct RangedWeaponViewSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffGameObjectRef>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        //Play Anim (Temp)
        foreach (var (goRef, data, entity) in SystemAPI
            .Query<StuffGameObjectRef, RefRW<DynamicData>>()
            .WithPresent<IsStuffInHand>()
            .WithEntityAccess())
        {
            switch (data.ValueRO.state)
            {
                case _State.Idle:
                    break;
                case _State.Shoot:
                    goRef.Value.GetComponent<Animator>().SetTrigger("Fire");
                    goRef.Value.GetComponent<VisualEffect>().Play();
                    data.ValueRW.state = _State.Idle;
                    break;
                case _State.Reload:
                    break;
                case _State.Droped:
                    break;
                default:
                    break;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}




