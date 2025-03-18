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
            ref CharacterStuffInHandType activeStuff = ref activeStuffRef.ValueRW;

            if (stuffList.List[0] == Entity.Null) continue; //TEMP

            if (input.selectNext.IsSet)
            {
                Debug.Log("Switch to Weapon up");
                Entity previousStuff = stuffList.List[(int)activeStuff.Value];
                ecb.RemoveComponent<StuffInHandTag>(previousStuff);
                ecb.RemoveComponent<WeaponViewPrefab>(previousStuff);
                StuffAnimatorRef stuffAnimator = state.EntityManager.GetComponentData<StuffAnimatorRef>(previousStuff);
                stuffAnimator.Animator.gameObject.SetActive(false);

                activeStuff.Value++;

                Entity nextStuff = stuffList.List[(int)activeStuff.Value];
                ecb.AddComponent(nextStuff, new StuffInHandTag());
                stuffAnimator = state.EntityManager.GetComponentData<StuffAnimatorRef>(nextStuff);
                stuffAnimator.Animator.gameObject.SetActive(true);
            }

            if (input.selectPrevious.IsSet)
            {
                //Debug.Log("Switch to Weapon down");
                //ecb.SetComponent(chara, new CharacterStuffInHandType { Value = stuffList.List.ElementAt(0) });
                //ecb.AddComponent(stuffList.List.ElementAt(0), new ActiveWeaponTag());
                //ecb.RemoveComponent<ActiveWeaponTag>(stuffList.List.ElementAt(1));
            }
        }
    }
}