using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct ServerClearAnimationBufferSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AnimatorReference>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (floatBuffer, intBuffer, boolBuffer, triggerBuffer, entity) in SystemAPI
            .Query<DynamicBuffer<AnimationFloatBufferElement>, DynamicBuffer<AnimationIntBufferElement>,
            DynamicBuffer<AnimationBoolBufferElement>, DynamicBuffer<AnimationTriggerBufferElement>>()
            .WithEntityAccess())
        {
            floatBuffer.Clear();
            intBuffer.Clear();
            boolBuffer.Clear();
            triggerBuffer.Clear();
        }
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial class ServerAnimationSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<AnimatorReference>();
    }

    protected override void OnUpdate()
    {
        foreach (var (animatorRef, floatBuffer, intBuffer, boolBuffer, triggerBuffer) in SystemAPI
            .Query<AnimatorReference, DynamicBuffer<AnimationFloatBufferElement>, DynamicBuffer<AnimationIntBufferElement>,
            DynamicBuffer<AnimationBoolBufferElement>, DynamicBuffer<AnimationTriggerBufferElement>>())
        {
            if (animatorRef.Animator == null) continue;

            AnimationUtils.UpdateFloatParameter(animatorRef.Animator, floatBuffer);
            AnimationUtils.UpdateIntParameter(animatorRef.Animator, intBuffer);
            AnimationUtils.UpdateBoolParameter(animatorRef.Animator, boolBuffer);
            AnimationUtils.UpdateTriggerParameter(animatorRef.Animator, triggerBuffer);
        }
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class ClientAnimationSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<AnimatorReference>();
    }

    protected override void OnUpdate()
    {
        foreach (var (animatorRef, floatBuffer, intBuffer, boolBuffer, triggerBuffer) in SystemAPI
            .Query<AnimatorReference, DynamicBuffer<AnimationFloatBufferElement>, DynamicBuffer<AnimationIntBufferElement>,
            DynamicBuffer<AnimationBoolBufferElement>, DynamicBuffer<AnimationTriggerBufferElement>>())
        {
            if (animatorRef.Animator == null) continue;

            AnimationUtils.UpdateFloatParameter(animatorRef.Animator, floatBuffer);
            AnimationUtils.UpdateIntParameter(animatorRef.Animator, intBuffer);
            AnimationUtils.UpdateBoolParameter(animatorRef.Animator, boolBuffer);
            AnimationUtils.UpdateTriggerParameter(animatorRef.Animator, triggerBuffer);

            floatBuffer.Clear();
            intBuffer.Clear();
            boolBuffer.Clear();
            triggerBuffer.Clear();
        }
    }
}
