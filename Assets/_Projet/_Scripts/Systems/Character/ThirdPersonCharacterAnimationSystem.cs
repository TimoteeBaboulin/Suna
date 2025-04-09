using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct ServerThirdPersonCharacterAnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ThirdPersonCharacterModelReference>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (modelReference, characterEntity) in SystemAPI
            .Query<ThirdPersonCharacterModelReference>()
            .WithNone<CharacterAnimatorReference>()
            .WithEntityAccess())
        {
            CommonCharacterAnimationUtils.SetAnimatorReference(modelReference.ModelGameObject, characterEntity, ecb, state.EntityManager);
        }

        foreach (var (animatorReference, thirdPersonAnimationState, commonAnimationState) in SystemAPI
            .Query<CharacterAnimatorReference, RefRO<ThirdPersonCharacterAnimationState>, RefRO<CommonCharacterAnimationState>>())
        {
            CommonCharacterAnimationUtils.SetParameters(animatorReference.Animator, commonAnimationState);
            ThirdPersonCharacterAnimationUtils.SetParameters(animatorReference.Animator, thirdPersonAnimationState);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct ClientThirdPersonCharacterAnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ThirdPersonCharacterModelReference>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (modelReference, characterEntity) in SystemAPI
            .Query<ThirdPersonCharacterModelReference>()
            .WithNone<CharacterAnimatorReference, GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            CommonCharacterAnimationUtils.SetAnimatorReference(modelReference.ModelGameObject, characterEntity, ecb, state.EntityManager);
        }

        foreach (var (animatorReference, animationState, commonAnimationState) in SystemAPI
            .Query<CharacterAnimatorReference, RefRO<ThirdPersonCharacterAnimationState>, RefRO<CommonCharacterAnimationState>>())
        {
            CommonCharacterAnimationUtils.SetParameters(animatorReference.Animator, commonAnimationState);
            ThirdPersonCharacterAnimationUtils.SetParameters(animatorReference.Animator, animationState);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
