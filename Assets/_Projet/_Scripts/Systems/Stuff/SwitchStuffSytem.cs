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
        foreach (var (stuffListRef, activeStuffRef, inputRef, chara) in SystemAPI
        .Query<RefRO<CharacterStuffList>, RefRW<CharacterStuffInHandLocation>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            ref readonly CharacterStuffList stuffList = ref stuffListRef.ValueRO;

            int dir = input.selectNext.IsSet ? 1 : input.selectPrevious.IsSet ? -1 : 0;

            if (dir != 0 && stuffList.List.Length > 1)
            {
                ref CharacterStuffInHandLocation stuffInHandLocation = ref activeStuffRef.ValueRW;

                Entity previousStuff = stuffList.List[(int)stuffInHandLocation.Value];
                Entity nextStuff = Entity.Null;

                for (int i = 0; i < stuffList.List.Length; i++)
                {
                    stuffInHandLocation.Value += dir;

                    stuffInHandLocation.Value = (StuffSlot)((stuffList.List.Length + (int)stuffInHandLocation.Value) % stuffList.List.Length);

                    nextStuff = stuffList.List[(int)stuffInHandLocation.Value];

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
        foreach (var (stuffListRef, stuffInHandTypeRef, inputRef, chara) in SystemAPI
        .Query<RefRO<CharacterStuffList>, RefRW<CharacterStuffInHandLocation>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            ref readonly CharacterStuffList stuffList = ref stuffListRef.ValueRO;

            if (input.stuffLocation > 0)
            {
                ref CharacterStuffInHandLocation stuffInHandLocation = ref stuffInHandTypeRef.ValueRW;
                int nextLocation = input.stuffLocation - 1;

                if ((int)stuffInHandLocation.Value != input.stuffLocation && stuffList.List.Length > 1)
                {
                    Entity previousStuff = stuffList.List[(int)stuffInHandLocation.Value];
                    Entity nextStuff = stuffList.List[nextLocation];

                    if (nextStuff != Entity.Null)
                    {
                        state.EntityManager.SetComponentEnabled<IsStuffInHand>(previousStuff, false);
                        state.EntityManager.SetComponentEnabled<IsStuffInHand>(nextStuff, true); //Conaard

                        stuffInHandLocation.Value = (StuffSlot)nextLocation;
                    }
                }
            }
        }

        //Auto Switch if Hands empty
        foreach (var (stuffListRef, stuffInHandTypeRef, chara) in SystemAPI
        .Query<RefRO<CharacterStuffList>, RefRW<CharacterStuffInHandLocation>>()
        .WithEntityAccess())
        {
            ref readonly CharacterStuffList stuffList = ref stuffListRef.ValueRO;

            if (stuffList.List.Length > 0)
            {
                ref CharacterStuffInHandLocation stuffInHandLocation = ref stuffInHandTypeRef.ValueRW;

                Entity previousStuff = stuffList.List[(int)stuffInHandLocation.Value];
                Entity nextStuff = Entity.Null;

                if (previousStuff == Entity.Null)
                {
                    for (int i = 0; i < stuffList.List.Length; i++)
                    {
                        nextStuff = stuffList.List[i];
                        stuffInHandLocation.Value = (StuffSlot)i;

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
