using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

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
        foreach (var (stuffList, stuffInfo, animatorRef, modelRef, entity) in SystemAPI
            .Query<DynamicBuffer<CharacterStuffList>, RefRO<CharacterStuffInfos>,
            AnimatorReference, FirstPersonCharacterModelReference>()
            .WithAll<CameraIsAtached>()
            .WithEntityAccess())
        {
            if (animatorRef.Animator == null) continue;

            Entity stuffInHand = StuffUtils.GetStuffInHand(stuffList, stuffInfo.ValueRO);

            if (stuffInHand == Entity.Null) continue;

            if (!EntityManager.HasComponent<StuffDatabaseAccess>(stuffInHand)) continue;

            FixedString128Bytes stuffName = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffInHand).NameInDatabase;
            SetAnimator(animatorRef.Animator, modelRef.AnimatorData, stuffName.ToString(), entity, ecb);
        }

        // For TPS model animator
        foreach (var (stuffList, stuffInfo, animatorRef, modelRef, entity) in SystemAPI
            .Query<DynamicBuffer<CharacterStuffList>, RefRO<CharacterStuffInfos>,
            AnimatorReference, ThirdPersonCharacterModelReference>()
            .WithNone<CameraIsAtached>()
            .WithEntityAccess())
        {
            if (animatorRef.Animator == null) continue;

            Entity stuffInHand = StuffUtils.GetStuffInHand(stuffList, stuffInfo.ValueRO);

            if (stuffInHand == Entity.Null)
            {
                if (animatorRef.Animator.runtimeAnimatorController != modelRef.AnimatorData.KnifeNeutral)
                {
                    animatorRef.Animator.runtimeAnimatorController = modelRef.AnimatorData.KnifeNeutral;
                }
            }

            if (!EntityManager.HasComponent<StuffDatabaseAccess>(stuffInHand)) continue;

            FixedString128Bytes stuffName = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffInHand).NameInDatabase;

            SetAnimator(animatorRef.Animator, modelRef.AnimatorData, stuffName.ToString(), entity, ecb);
        }

        foreach (var (ghostOwner, entity) in SystemAPI
            .Query<RefRO<GhostOwner>>()
            .WithAll<CharacterComponent, GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            ecb.SetComponentEnabled<CharacterIsDifusing>(entity, false);
            AnimationUtils.AddBoolCommand("IsDifuse", false, entity, ecb);

            ecb.SetComponentEnabled<CharacterIsPlanting>(entity, false);
            AnimationUtils.AddBoolCommand("IsPlant", false, entity, ecb);
        }

        foreach (var defusing in SystemAPI
            .Query<RefRO<HarvesterDefusing>>()
            .WithAll<HarvesterComponent>())
        {
            if (defusing.ValueRO.Defuser == Entity.Null) continue;
            if (!EntityManager.IsComponentEnabled<GhostOwnerIsLocal>(defusing.ValueRO.Defuser)) continue;

            if (EntityManager.HasComponent<CharacterIsDifusing>(defusing.ValueRO.Defuser))
            {
                ecb.SetComponentEnabled<CharacterIsDifusing>(defusing.ValueRO.Defuser, true);
                AnimationUtils.AddBoolCommand("IsDifuse", true, defusing.ValueRO.Defuser, ecb);
            }
        }

        foreach (var (harvester, owner, entity) in SystemAPI
            .Query<RefRO<HarvesterComponent>, RefRO<StuffDynamicData>>()
            .WithAll<HarvesterPlanting>()
            .WithEntityAccess())
        {
            if (owner.ValueRO.owner == Entity.Null) continue;
            if (!EntityManager.IsComponentEnabled<GhostOwnerIsLocal>(owner.ValueRO.owner)) continue;

            if (EntityManager.HasComponent<CharacterIsPlanting>(owner.ValueRO.owner))
            {
                ecb.SetComponentEnabled<CharacterIsPlanting>(owner.ValueRO.owner, true);
                AnimationUtils.AddBoolCommand("IsPlant", true, owner.ValueRO.owner, ecb);
            }
        }

        foreach (var (character, entity) in SystemAPI
            .Query<RefRO<CharacterComponent>>()
            .WithAll<GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            if (EntityManager.IsComponentEnabled<CharacterIsEnable>(entity))
            {
                AnimationUtils.AddBoolCommand("IsDead", false, entity, ecb);
            }
            else
            {
                AnimationUtils.AddBoolCommand("IsDead", true, entity, ecb);
            }
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private void SetAnimator(Animator animator, ModelAnimatorData animatorData, string stuffName,
        Entity entity, EntityCommandBuffer ecb)
    {
        if (stuffName == "Banduka")
        {
            if (animator.runtimeAnimatorController != animatorData.Banduka)
            {
                animator.runtimeAnimatorController = animatorData.Banduka;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "Decimator")
        {
            if (animator.runtimeAnimatorController != animatorData.Decimator)
            {
                animator.runtimeAnimatorController = animatorData.Decimator;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "Fakir")
        {
            if (animator.runtimeAnimatorController != animatorData.Fakir)
            {
                animator.runtimeAnimatorController = animatorData.Fakir;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "HE Grenade")
        {
            if (animator.runtimeAnimatorController != animatorData.Grenade_Base)
            {
                animator.runtimeAnimatorController = animatorData.Grenade_Base;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "Grenade_Fire")
        {
            if (animator.runtimeAnimatorController != animatorData.Grenade_Fire)
            {
                animator.runtimeAnimatorController = animatorData.Grenade_Fire;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "Flashbang")
        {
            if (animator.runtimeAnimatorController != animatorData.Grenade_Flash)
            {
                animator.runtimeAnimatorController = animatorData.Grenade_Flash;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "Gas grenade")
        {
            if (animator.runtimeAnimatorController != animatorData.Grenade_Gas)
            {
                animator.runtimeAnimatorController = animatorData.Grenade_Gas;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "Smoke Grenade")
        {
            if (animator.runtimeAnimatorController != animatorData.Grenade_Smoke)
            {
                animator.runtimeAnimatorController = animatorData.Grenade_Smoke;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "Harvester")
        {
            if (animator.runtimeAnimatorController != animatorData.Harvester)
            {
                animator.runtimeAnimatorController = animatorData.Harvester;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "KnifeNeutral")
        {
            if (animator.runtimeAnimatorController != animatorData.KnifeNeutral)
            {
                animator.runtimeAnimatorController = animatorData.KnifeNeutral;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "Laksya")
        {
            if (animator.runtimeAnimatorController != animatorData.Laksya)
            {
                animator.runtimeAnimatorController = animatorData.Laksya;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "LP17")
        {
            if (animator.runtimeAnimatorController != animatorData.LP17)
            {
                animator.runtimeAnimatorController = animatorData.LP17;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "SKAR18")
        {
            if (animator.runtimeAnimatorController != animatorData.SKAR18)
            {
                animator.runtimeAnimatorController = animatorData.SKAR18;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
        else if (stuffName == "Nelara")
        {
            if (animator.runtimeAnimatorController != animatorData.Nelara)
            {
                animator.runtimeAnimatorController = animatorData.Nelara;
                AnimationUtils.AddTriggerCommand("Change", entity, ecb);
            }
        }
    }
}
