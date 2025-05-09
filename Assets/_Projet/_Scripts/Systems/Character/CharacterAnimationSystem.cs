using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial class ServerCharacterAnimationSystem : SystemBase
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffDatabaseAccess>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        // For FPS model animator
        foreach (var (stuffList, stuffInfo, ghostOwner, animatorRef, modelRef, entity) in SystemAPI
            .Query<DynamicBuffer<CharacterStuffList>, RefRO<CharacterStuffInfos>, RefRO<GhostOwner>,
            AnimatorReference, FirstPersonCharacterModelReference>()
            .WithEntityAccess())
        {
            Entity stuffInHand = StuffUtils.GetStuffInHand(stuffList, stuffInfo.ValueRO);

            if (stuffInHand == Entity.Null) continue;

            if (!EntityManager.HasComponent<StuffDatabaseAccess>(stuffInHand)) continue;

            FixedString128Bytes stuffName = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffInHand).NameInDatabase;

            //SetAnimator(animatorRef.Animator, modelRef.AnimatorData, stuffName.ToString(), entity, ecb, ghostOwner.ValueRO.NetworkId);
        }

        // For TPS model animator
        foreach (var (stuffList, stuffInfo, ghostOwner, animatorRef, modelRef, entity) in SystemAPI
            .Query<DynamicBuffer<CharacterStuffList>, RefRO<CharacterStuffInfos>, RefRO<GhostOwner>,
            AnimatorReference, ThirdPersonCharacterModelReference>()
            .WithEntityAccess())
        {
            Entity stuffInHand = StuffUtils.GetStuffInHand(stuffList, stuffInfo.ValueRO);

            if (stuffInHand == Entity.Null) continue;

            if (!EntityManager.HasComponent<StuffDatabaseAccess>(stuffInHand)) continue;

            FixedString128Bytes stuffName = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffInHand).NameInDatabase;

            SetAnimator(animatorRef.Animator, modelRef.AnimatorData, stuffName.ToString(), entity, ecb, ghostOwner.ValueRO.NetworkId);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private void SetAnimator(Animator animator, ModelAnimatorData animatorData, string stuffName,
        Entity entity, EntityCommandBuffer ecb, int networkId)
    {
        if (stuffName == "Banduka")
        {
            if (animator.runtimeAnimatorController != animatorData.Banduka)
            {
                animator.runtimeAnimatorController = animatorData.Banduka;
            }
        }
        else if (stuffName == "Decimator")
        {
            if (animator.runtimeAnimatorController != animatorData.Decimator)
            {
                animator.runtimeAnimatorController = animatorData.Decimator;
            }
        }
        else if (stuffName == "Fakir")
        {
            if (animator.runtimeAnimatorController != animatorData.Fakir)
            {
                animator.runtimeAnimatorController = animatorData.Fakir;
            }
        }
        else if (stuffName == "Grenade_Base")
        {
            if (animator.runtimeAnimatorController != animatorData.Grenade_Base)
            {
                animator.runtimeAnimatorController = animatorData.Grenade_Base;
            }
        }
        else if (stuffName == "Grenade_Fire")
        {
            if (animator.runtimeAnimatorController != animatorData.Grenade_Fire)
            {
                animator.runtimeAnimatorController = animatorData.Grenade_Fire;
            }
        }
        else if (stuffName == "Grenade_Flash")
        {
            if (animator.runtimeAnimatorController != animatorData.Grenade_Flash)
            {
                animator.runtimeAnimatorController = animatorData.Grenade_Flash;
            }
        }
        else if (stuffName == "Grenade_Gas")
        {
            if (animator.runtimeAnimatorController != animatorData.Grenade_Gas)
            {
                animator.runtimeAnimatorController = animatorData.Grenade_Gas;
            }
        }
        else if (stuffName == "Grenade_Smoke")
        {
            if (animator.runtimeAnimatorController != animatorData.Grenade_Smoke)
            {
                animator.runtimeAnimatorController = animatorData.Grenade_Smoke;
            }
        }
        else if (stuffName == "Harvester")
        {
            if (animator.runtimeAnimatorController != animatorData.Harvester)
            {
                animator.runtimeAnimatorController = animatorData.Harvester;
            }
        }
        else if (stuffName == "KnifeNeutral")
        {
            if (animator.runtimeAnimatorController != animatorData.KnifeNeutral)
            {
                animator.runtimeAnimatorController = animatorData.KnifeNeutral;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb, networkId);
            }
        }
        else if (stuffName == "Laksya")
        {
            if (animator.runtimeAnimatorController != animatorData.Laksya)
            {
                animator.runtimeAnimatorController = animatorData.Laksya;
            }
        }
        else if (stuffName == "LP17")
        {
            if (animator.runtimeAnimatorController != animatorData.LP17)
            {
                animator.runtimeAnimatorController = animatorData.LP17;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb, networkId);
            }
        }
        else if (stuffName == "SKAR18")
        {
            if (animator.runtimeAnimatorController != animatorData.SKAR18)
            {
                animator.runtimeAnimatorController = animatorData.SKAR18;
            }
        }
        else if (stuffName == "Nelara")
        {
            if (animator.runtimeAnimatorController != animatorData.Nelara)
            {
                animator.runtimeAnimatorController = animatorData.Nelara;
            }
        }
    }
}
