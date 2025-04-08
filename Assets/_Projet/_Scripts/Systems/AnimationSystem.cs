using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
partial class CommonAnimationSystem : SystemBase
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
