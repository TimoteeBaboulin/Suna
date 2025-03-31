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
            .WithNone<ThirdPersonCharacterAnimatorReference>()
            .WithEntityAccess())
        {
            ThirdPersonCharacterAnimationUtils.AddAnimatorReferenceComponent(modelReference.ModelGameObject, characterEntity, ecb);
        }

        foreach (var (animatorReference, animationState) in SystemAPI
            .Query<ThirdPersonCharacterAnimatorReference, RefRO<ThirdPersonCharacterAnimationState>>())
        {
            ThirdPersonCharacterAnimationUtils.SetParameters(animatorReference.Animator, animationState);
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
            .WithNone<ThirdPersonCharacterAnimatorReference, GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            ThirdPersonCharacterAnimationUtils.AddAnimatorReferenceComponent(modelReference.ModelGameObject, characterEntity, ecb);
        }

        foreach (var (animatorReference, animationState) in SystemAPI
            .Query<ThirdPersonCharacterAnimatorReference, RefRO<ThirdPersonCharacterAnimationState>>())
        {
            ThirdPersonCharacterAnimationUtils.SetParameters(animatorReference.Animator, animationState);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
