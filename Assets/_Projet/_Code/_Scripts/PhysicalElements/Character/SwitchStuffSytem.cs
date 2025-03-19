using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct SwitchStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<StuffOwner>();
    }

    public void OnUpdate(ref SystemState state)
    {
        //Eviter répétition sur le serveur du a la différence de framerate avec le client
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.IsFirstPredictionTick) return;

        float dt = networkTime.ServerTickFraction * SystemAPI.Time.DeltaTime;
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        foreach (var (stuffListRef, activeStuffRef, inputRef, chara) in SystemAPI
        .Query<RefRO<CharacterStuffList>, RefRW<CharacterStuffInHandType>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            //Simplification des components de l'arme
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            ref readonly CharacterStuffList stuffList = ref stuffListRef.ValueRO;
            ref CharacterStuffInHandType stuffInHandType = ref activeStuffRef.ValueRW;

            if (input.selectNext.IsSet)
            {
                Entity previousStuff = stuffList.List[(int)stuffInHandType.Value];
                Debug.Log(stuffList.List[(int)stuffInHandType.Value] + "  " + stuffInHandType.Value); //TODO : Fixe Rare error
                state.EntityManager.SetComponentEnabled<IsStuffInHand>(previousStuff, false);

                int whileLimit = 0;
                Entity nextStuff;

                do
                {
                    stuffInHandType.Value++;

                    if ((int)stuffInHandType.Value >= stuffList.List.Length)
                    {
                        stuffInHandType.Value = 0;
                    }

                    nextStuff = stuffList.List[(int)stuffInHandType.Value];

                    whileLimit++;

                } while (nextStuff == Entity.Null && whileLimit < stuffList.List.Length);

                state.EntityManager.SetComponentEnabled<IsStuffInHand>(nextStuff, true);
            }

            else if (input.selectPrevious.IsSet)
            {
                Entity previousStuff = stuffList.List[(int)stuffInHandType.Value];
                state.EntityManager.SetComponentEnabled<IsStuffInHand>(previousStuff, false);

                int whileLimit = 0;
                Entity nextStuff;

                do
                {
                    stuffInHandType.Value--;

                    if ((int)stuffInHandType.Value < 0)
                    {
                        stuffInHandType.Value = (StuffType)(stuffList.List.Length - 1);
                    }

                    nextStuff = stuffList.List[(int)stuffInHandType.Value];

                    whileLimit++;

                } while (nextStuff == Entity.Null && whileLimit < stuffList.List.Length);

                state.EntityManager.SetComponentEnabled<IsStuffInHand>(nextStuff, true);
            }


        }
    }
}