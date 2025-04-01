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

            if (dir != 0 && stuffList.Value.Length > 1)
            {
                ref CharacterStuffInHandLocation stuffInHandLocation = ref activeStuffRef.ValueRW;

                Entity previousStuff = stuffList.Value[(int)stuffInHandLocation.Value];
                Entity nextStuff = Entity.Null;

                for (int i = 0; i < stuffList.Value.Length; i++)
                {
                    stuffInHandLocation.Value += dir;

                    stuffInHandLocation.Value = (StuffInventoryLocation)((stuffList.Value.Length + (int)stuffInHandLocation.Value) % stuffList.Value.Length);

                    nextStuff = stuffList.Value[(int)stuffInHandLocation.Value];

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

                if ((int)stuffInHandLocation.Value != input.stuffLocation && stuffList.Value.Length > 1)
                {
                    Entity previousStuff = stuffList.Value[(int)stuffInHandLocation.Value];
                    Entity nextStuff = stuffList.Value[nextLocation];

                    if (nextStuff != Entity.Null)
                    {
                        state.EntityManager.SetComponentEnabled<IsStuffInHand>(previousStuff, false);
                        state.EntityManager.SetComponentEnabled<IsStuffInHand>(nextStuff, true); //Conaard

                        stuffInHandLocation.Value = (StuffInventoryLocation)nextLocation;
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

            if (stuffList.Value.Length > 0)
            {
                ref CharacterStuffInHandLocation stuffInHandLocation = ref stuffInHandTypeRef.ValueRW;

                Entity previousStuff = stuffList.Value[(int)stuffInHandLocation.Value];
                Entity nextStuff = Entity.Null;

                if (previousStuff == Entity.Null)
                {
                    for (int i = 0; i < stuffList.Value.Length; i++)
                    {
                        nextStuff = stuffList.Value[i];
                        stuffInHandLocation.Value = (StuffInventoryLocation)i;

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
