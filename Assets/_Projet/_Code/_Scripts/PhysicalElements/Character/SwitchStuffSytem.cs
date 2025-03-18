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
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

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
                ecb.SetComponentEnabled<IsStuffInHand>(previousStuff, false);

                stuffInHandType.Value++;

                Entity nextStuff = stuffList.List[(int)stuffInHandType.Value];
                ecb.SetComponentEnabled<IsStuffInHand>(nextStuff, true);
            }

            if (input.selectPrevious.IsSet)
            {
                Entity previousStuff = stuffList.List[(int)stuffInHandType.Value];
                ecb.SetComponentEnabled<IsStuffInHand>(previousStuff, false);

                stuffInHandType.Value--;

                Entity nextStuff = stuffList.List[(int)stuffInHandType.Value];
                ecb.SetComponentEnabled<IsStuffInHand>(nextStuff, true);
            }
        }
    }
}