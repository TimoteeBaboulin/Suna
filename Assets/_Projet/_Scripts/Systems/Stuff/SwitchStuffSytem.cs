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
        foreach (var (stuffListRef, inputRef, chara) in SystemAPI
        .Query<RefRW<CharacterStuffList>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            ref CharacterStuffList stuffList = ref stuffListRef.ValueRW;

            int dir = input.selectNext.IsSet ? 1 : input.selectPrevious.IsSet ? -1 : 0;

            if (dir != 0 && stuffList.List.Length > 1)
            {
                Entity previousStuff = stuffList.StuffInHand;
                StuffSlot nextStuffSlot = stuffList.StuffInHandSlot;

                for (int i = 0; i < stuffList.List.Length; i++)
                {
                    nextStuffSlot += dir;
                    nextStuffSlot = (StuffSlot)((stuffList.List.Length + (int)nextStuffSlot) % stuffList.List.Length);

                    if (stuffList.GetStuffInSlot(nextStuffSlot) != Entity.Null) break;
                }

                StuffUtils.SwitchTo(ref state, ref stuffList, nextStuffSlot);
            }
        }

        ////Switch with shortcut
        foreach (var (stuffListRef, inputRef, chara) in SystemAPI
        .Query<RefRW<CharacterStuffList>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            ref CharacterStuffList stuffList = ref stuffListRef.ValueRW;

            if (stuffList.List.Length <= 1) continue;

            if (input.stuffLocation > 0)
            {
                if ((int)stuffList.StuffInHandSlot == input.stuffLocation) continue;

                StuffUtils.SwitchTo(ref state, ref stuffList, (StuffSlot)(input.stuffLocation - 1));
            }
        }

        //Auto Switch if Hands empty
        foreach (var (stuffListRef, chara) in SystemAPI
        .Query<RefRW<CharacterStuffList>>()
        .WithEntityAccess())
        {
            ref CharacterStuffList stuffList = ref stuffListRef.ValueRW;

            if (stuffList.StuffInHand == Entity.Null)
            {
                for (int i = 0; i < stuffList.List.Length; i++)
                {
                    if (stuffList.GetStuffInSlot((StuffSlot)i) == Entity.Null) continue;

                    StuffUtils.SwitchTo(ref state, ref stuffList, (StuffSlot)i);
                    break;
                }
            }
        }
    }
}
