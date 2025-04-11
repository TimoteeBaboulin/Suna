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
        state.RequireForUpdate<StuffOwner>();

        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<PhysicsWorldHistorySingleton>();
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
                
                Entity previousStuff = stuffList.List[(int)stuffList.StuffInHandSlot];
                Entity nextStuff = Entity.Null;

                for (int i = 0; i < stuffList.List.Length; i++)
                {
                    stuffList.StuffInHandSlot += dir;

                    stuffList.StuffInHandSlot = (StuffSlot)((stuffList.List.Length + (int)stuffList.StuffInHandSlot) % stuffList.List.Length);

                    nextStuff = stuffList.List[(int)stuffList.StuffInHandSlot];

                    if (nextStuff != Entity.Null) break;
                }

                if (nextStuff != Entity.Null)
                {
                    state.EntityManager.SetComponentEnabled<IsStuffInHand>(previousStuff, false);
                    state.EntityManager.SetComponentEnabled<IsStuffInHand>(nextStuff, true);
                }
            }
        }

        ////Switch with shortcut
        foreach (var (stuffListRef, inputRef, chara) in SystemAPI
        .Query<RefRW<CharacterStuffList>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            ref CharacterStuffList stuffList = ref stuffListRef.ValueRW;
            if (input.stuffLocation > 0)
            {
                int nextLocation = input.stuffLocation - 1;

                if ((int)stuffList.StuffInHandSlot != input.stuffLocation && stuffList.List.Length > 1)
                {
                    Entity previousStuff = stuffList.List[(int)stuffList.StuffInHandSlot];
                    Entity nextStuff = stuffList.List[nextLocation];

                    if (nextStuff != Entity.Null)
                    {
                        state.EntityManager.SetComponentEnabled<IsStuffInHand>(previousStuff, false);
                        state.EntityManager.SetComponentEnabled<IsStuffInHand>(nextStuff, true); //Conaard

                        stuffList.StuffInHandSlot = (StuffSlot)nextLocation;
                    }
                }
            }
        }

        //Auto Switch if Hands empty
        foreach (var (stuffListRef, chara) in SystemAPI
        .Query<RefRW<CharacterStuffList>>()
        .WithEntityAccess())
        {
            ref CharacterStuffList stuffList = ref stuffListRef.ValueRW;

            if (stuffList.List.Length > 0)
            {
                Entity previousStuff = stuffList.List[(int)stuffList.StuffInHandSlot];
                Entity nextStuff = Entity.Null;

                if (previousStuff == Entity.Null)
                {
                    for (int i = 0; i < stuffList.List.Length; i++)
                    {
                        nextStuff = stuffList.List[i];
                        stuffList.StuffInHandSlot = (StuffSlot)i;

                        if (nextStuff != Entity.Null)
                        {
                            state.EntityManager.SetComponentEnabled<IsStuffInHand>(nextStuff, true);
                            break;
                        }
                    }
                }
                else if (!state.EntityManager.IsComponentEnabled<IsStuffInHand>(previousStuff))
                {
                    state.EntityManager.SetComponentEnabled<IsStuffInHand>(previousStuff, true);
                }
            }
        }
    }
}
