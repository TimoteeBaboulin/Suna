using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct SwitchStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffDynamicData>();

        state.RequireForUpdate<NetworkTime>();

        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        //Switch with mouseScroll
        foreach (var (stuffList, stuffInfosRW, inputRef, chara) in SystemAPI
        .Query<DynamicBuffer<CharacterStuffList>, RefRW<CharacterStuffInfos>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            ref CharacterStuffInfos stuffInfos = ref stuffInfosRW.ValueRW;

            int dir = input.selectNext.IsSet ? 1 : input.selectPrevious.IsSet ? -1 : 0;

            if (dir != 0 && stuffList.Length > 1)
            {
                Entity previousStuff = StuffUtils.GetStuffInHand(stuffList, stuffInfos);
                StuffSlot nextStuffSlot = stuffInfos.StuffInHandSlot;

                for (int i = 0; i < stuffList.Length; i++)
                {
                    nextStuffSlot += dir;
                    nextStuffSlot = (StuffSlot)((stuffList.Length + (int)nextStuffSlot) % stuffList.Length);

                    if (StuffUtils.GetStuffInSlot(stuffList, nextStuffSlot) != Entity.Null) break;
                }

                StuffUtils.SwitchTo(stuffList, stuffInfosRW, nextStuffSlot);
            }
        }

        //Switch with shortcut
        foreach (var (stuffList, stuffInfosRW, inputRef, chara) in SystemAPI
        .Query<DynamicBuffer<CharacterStuffList>, RefRW<CharacterStuffInfos>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            ref CharacterStuffInfos stuffInfos = ref stuffInfosRW.ValueRW;

            if (stuffList.Length <= 1) continue;

            if (input.stuffLocation > 0)
            {
                if ((int)stuffInfos.StuffInHandSlot == input.stuffLocation - 1) continue;

                StuffUtils.SwitchTo(stuffList, stuffInfosRW, (StuffSlot)(input.stuffLocation - 1));
            }
        }

        //Auto Switch if Hands empty
        foreach (var (stuffList, stuffInfosRW, chara) in SystemAPI
        .Query<DynamicBuffer<CharacterStuffList>, RefRW<CharacterStuffInfos>>()
        .WithEntityAccess())
        {
            if (StuffUtils.GetStuffInHand(stuffList, stuffInfosRW.ValueRW) == Entity.Null)
            {
                for (int i = 0; i < stuffList.Length; i++)
                {
                    if (StuffUtils.GetStuffInSlot(stuffList, (StuffSlot)i) == Entity.Null) continue;

                    StuffUtils.SwitchTo(stuffList, stuffInfosRW, (StuffSlot)i);
                    break;
                }
            }
        }

        //Auto enable/disable IsStuffInHand
        foreach (var (dynData, stuff) in SystemAPI
        .Query<RefRW<StuffDynamicData>>()
        .WithPresent<IsStuffInHand>()
        .WithEntityAccess())
        {
            if (dynData.ValueRO.owner != Entity.Null)
            {
                Entity stuffInHand = StuffUtils.GetStuffInHandUnsafe(ref state, dynData.ValueRO.owner);
                state.EntityManager.SetComponentEnabled<IsStuffInHand>(stuff, stuffInHand == stuff);
            }
            else
            {
                state.EntityManager.SetComponentEnabled<IsStuffInHand>(stuff, false);
            }
        }
    }
}
