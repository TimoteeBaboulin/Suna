using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

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

            AnimationUtils.UpdateServerFloatParameter(animatorRef.Animator, floatBuffer);
            AnimationUtils.UpdateServerIntParameter(animatorRef.Animator, intBuffer);
            AnimationUtils.UpdateServerBoolParameter(animatorRef.Animator, boolBuffer);
            AnimationUtils.UpdateServerTriggerParameter(animatorRef.Animator, triggerBuffer);

            floatBuffer.Clear();
            intBuffer.Clear();
            boolBuffer.Clear();
            triggerBuffer.Clear();
        }
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class ClientLocalAnimationSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<AnimatorReference>();
    }

    protected override void OnUpdate()
    {
        foreach (var (animatorRef, floatBuffer, intBuffer, boolBuffer, triggerBuffer) in SystemAPI
            .Query<AnimatorReference, DynamicBuffer<AnimationFloatBufferElement>, DynamicBuffer<AnimationIntBufferElement>,
            DynamicBuffer<AnimationBoolBufferElement>, DynamicBuffer<AnimationTriggerBufferElement>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            if (animatorRef.Animator == null) continue;

            AnimationUtils.UpdateClientFloatParameter(animatorRef.Animator, floatBuffer);
            AnimationUtils.UpdateClientIntParameter(animatorRef.Animator, intBuffer);
            AnimationUtils.UpdateClientBoolParameter(animatorRef.Animator, boolBuffer);
            AnimationUtils.UpdateClientTriggerParameter(animatorRef.Animator, triggerBuffer);

            floatBuffer.Clear();
            intBuffer.Clear();
            boolBuffer.Clear();
            triggerBuffer.Clear();
        }

        foreach (var (animatorRef, floatBuffer, intBuffer, boolBuffer, triggerBuffer) in SystemAPI
            .Query<AnimatorReference, DynamicBuffer<AnimationFloatBufferElement>, DynamicBuffer<AnimationIntBufferElement>,
            DynamicBuffer<AnimationBoolBufferElement>, DynamicBuffer<AnimationTriggerBufferElement>>()
            .WithNone<GhostOwnerIsLocal>())
        {
            floatBuffer.Clear();
            intBuffer.Clear();
            boolBuffer.Clear();
            triggerBuffer.Clear();
        }
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class ClientRpcAnimationSystem : SystemBase
{
    protected override void OnCreate()
    {
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (rpc, entity) in SystemAPI
            .Query<RefRO<FloatParameterRpc>>()
            .WithAll<ReceiveRpcCommandRequest>()
            .WithEntityAccess())
        {
            foreach (var (animatorRef, ghostOwner) in SystemAPI
                .Query<AnimatorReference, RefRO<GhostOwner>>()
                .WithNone<GhostOwnerIsLocal>())
            {
                if (ghostOwner.ValueRO.NetworkId != rpc.ValueRO.NetworkId) continue;

                animatorRef.Animator.SetFloat(rpc.ValueRO.Parameter.ToString(), rpc.ValueRO.Value);
                break;
            }

            ecb.DestroyEntity(entity);
        }

        foreach (var (rpc, entity) in SystemAPI
            .Query<RefRO<IntParameterRpc>>()
            .WithAll<ReceiveRpcCommandRequest>()
            .WithEntityAccess())
        {
            foreach (var (animatorRef, ghostOwner) in SystemAPI
                .Query<AnimatorReference, RefRO<GhostOwner>>()
                .WithNone<GhostOwnerIsLocal>())
            {
                if (ghostOwner.ValueRO.NetworkId != rpc.ValueRO.NetworkId) continue;

                animatorRef.Animator.SetInteger(rpc.ValueRO.Parameter.ToString(), rpc.ValueRO.Value);
                break;
            }

            ecb.DestroyEntity(entity);
        }

        foreach (var (rpc, entity) in SystemAPI
            .Query<RefRO<BoolParameterRpc>>()
            .WithAll<ReceiveRpcCommandRequest>()
            .WithEntityAccess())
        {
            foreach (var (animatorRef, ghostOwner) in SystemAPI
                .Query<AnimatorReference, RefRO<GhostOwner>>()
                .WithNone<GhostOwnerIsLocal>())
            {
                if (ghostOwner.ValueRO.NetworkId != rpc.ValueRO.NetworkId) continue;

                animatorRef.Animator.SetBool(rpc.ValueRO.Parameter.ToString(), rpc.ValueRO.Value);
                break;
            }

            ecb.DestroyEntity(entity);
        }

        foreach (var (rpc, entity) in SystemAPI
            .Query<RefRO<TriggerParameterRpc>>()
            .WithAll<ReceiveRpcCommandRequest>()
            .WithEntityAccess())
        {
            foreach (var (animatorRef, ghostOwner) in SystemAPI
                .Query<AnimatorReference, RefRO<GhostOwner>>()
                .WithNone<GhostOwnerIsLocal>())
            {
                if (ghostOwner.ValueRO.NetworkId != rpc.ValueRO.NetworkId) continue;

                animatorRef.Animator.SetTrigger(rpc.ValueRO.Parameter.ToString());
                break;
            }

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
