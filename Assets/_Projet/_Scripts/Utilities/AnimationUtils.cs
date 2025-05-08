using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class AnimationUtils
{
    // Only used in Animation System
    public static void UpdateClientFloatParameter(in Animator animator, DynamicBuffer<AnimationFloatBufferElement> floatBuffer)
    {
        foreach (var floatElement in floatBuffer)
        {
            animator.SetFloat(floatElement.Parameter.ToString(), floatElement.Value);
        }
    }

    // Only used in Animation System
    public static void UpdateClientIntParameter(in Animator animator, DynamicBuffer<AnimationIntBufferElement> intBuffer)
    {
        foreach (var intElement in intBuffer)
        {
            animator.SetInteger(intElement.Parameter.ToString(), intElement.Value);
        }
    }

    // Only used in Animation System
    public static void UpdateClientBoolParameter(in Animator animator, DynamicBuffer<AnimationBoolBufferElement> boolBuffer)
    {
        foreach (var boolElement in boolBuffer)
        {
            animator.SetBool(boolElement.Parameter.ToString(), boolElement.Value);
        }
    }

    // Only used in Animation System
    public static void UpdateClientTriggerParameter(in Animator animator, DynamicBuffer<AnimationTriggerBufferElement> triggerBuffer)
    {
        foreach (var triggerElement in triggerBuffer)
        {
            animator.SetTrigger(triggerElement.Parameter.ToString());
        }
    }

    // Only used in Animation System
    public static void UpdateServerFloatParameter(in Animator animator, DynamicBuffer<AnimationFloatBufferElement> floatBuffer)
    {
        foreach (var floatElement in floatBuffer)
        {
            animator.SetFloat(floatElement.Parameter.ToString(), floatElement.Value);

            FloatParameterRpc rpc = new FloatParameterRpc
            {
                NetworkId = floatElement.NetworkId,
                Parameter = floatElement.Parameter,
                Value = floatElement.Value,
            };
            RpcUtils.SendServerToClientRpc(ref rpc);
        }
    }

    // Only used in Animation System
    public static void UpdateServerIntParameter(in Animator animator, DynamicBuffer<AnimationIntBufferElement> intBuffer)
    {
        foreach (var intElement in intBuffer)
        {
            animator.SetInteger(intElement.Parameter.ToString(), intElement.Value);

            IntParameterRpc rpc = new IntParameterRpc
            {
                NetworkId = intElement.NetworkId,
                Parameter = intElement.Parameter,
                Value = intElement.Value,
            };
            RpcUtils.SendServerToClientRpc(ref rpc);
        }
    }

    // Only used in Animation System
    public static void UpdateServerBoolParameter(in Animator animator, DynamicBuffer<AnimationBoolBufferElement> boolBuffer)
    {
        foreach (var boolElement in boolBuffer)
        {
            animator.SetBool(boolElement.Parameter.ToString(), boolElement.Value);

            BoolParameterRpc rpc = new BoolParameterRpc
            {
                NetworkId = boolElement.NetworkId,
                Parameter = boolElement.Parameter,
                Value = boolElement.Value,
            };
            RpcUtils.SendServerToClientRpc(ref rpc);
        }
    }

    // Only used in Animation System
    public static void UpdateServerTriggerParameter(in Animator animator, DynamicBuffer<AnimationTriggerBufferElement> triggerBuffer)
    {
        foreach (var triggerElement in triggerBuffer)
        {
            animator.SetTrigger(triggerElement.Parameter.ToString());

            TriggerParameterRpc rpc = new TriggerParameterRpc
            {
                NetworkId = triggerElement.NetworkId,
                Parameter = triggerElement.Parameter,
            };
            RpcUtils.SendServerToClientRpc(ref rpc);
        }
    }

    // Only used in Animation System
    public static void SetAnimator(in Animator animator, in Entity entity, in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        if (entityManager.HasComponent<AnimatorReference>(entity))
        {
            AnimatorReference animatorRef = entityManager.GetComponentObject<AnimatorReference>(entity);

            if (animatorRef.Animator != animator)
            {
                animatorRef.Animator = animator;
            }
        }
    }

    // To be used to set a parameter outside of a job
    [BurstCompile]
    public static void AddFloatCommand(in FixedString32Bytes name, in float value, in Entity entity, 
        in EntityCommandBuffer ecb, in int networkId)
    {
        ecb.AppendToBuffer(entity, new AnimationFloatBufferElement
        {
            NetworkId = networkId,
            Parameter = name,
            Value = value,
        });
    }

    // To be used to set a parameter in a job
    [BurstCompile]
    public static void AddFloatCommandJob(in FixedString32Bytes name, in float value, in Entity entity, 
        in EntityCommandBuffer.ParallelWriter ecb, in int sortKey, in int networkId)
    {
        ecb.AppendToBuffer(sortKey, entity, new AnimationFloatBufferElement
        {
            NetworkId = networkId,
            Parameter = name,
            Value = value,
        });
    }

    // To be used to set a parameter outside of a job
    [BurstCompile]
    public static void AddIntCommand(in FixedString32Bytes name, in int value, in Entity entity, 
        in EntityCommandBuffer ecb, in int networkId)
    {
        ecb.AppendToBuffer(entity, new AnimationIntBufferElement
        {
            NetworkId = networkId,
            Parameter = name,
            Value = value,
        });
    }

    // To be used to set a parameter in a job
    [BurstCompile]
    public static void AddIntCommandJob(in FixedString32Bytes name, in int value, in Entity entity, 
        in EntityCommandBuffer.ParallelWriter ecb, in int sortKey, in int networkId)
    {
        ecb.AppendToBuffer(sortKey, entity, new AnimationIntBufferElement
        {
            NetworkId = networkId,
            Parameter = name,
            Value = value,
        });
    }

    // To be used to set a parameter outside of a job
    [BurstCompile]
    public static void AddBoolCommand(in FixedString32Bytes name, in bool value, in Entity entity, 
        in EntityCommandBuffer ecb, in int networkId)
    {
        ecb.AppendToBuffer(entity, new AnimationBoolBufferElement
        {
            NetworkId = networkId,
            Parameter = name,
            Value = value,
        });
    }

    // To be used to set a parameter in a job
    [BurstCompile]
    public static void AddBoolCommandJob(in FixedString32Bytes name, in bool value, in Entity entity, 
        in EntityCommandBuffer.ParallelWriter ecb, in int sortKey, in int networkId)
    {
        ecb.AppendToBuffer(sortKey, entity, new AnimationBoolBufferElement
        {
            NetworkId = networkId,
            Parameter = name,
            Value = value,
        });
    }

    // To be used to set a parameter outside of a job
    [BurstCompile]
    public static void AddTriggerCommand(in FixedString32Bytes name, in Entity entity, 
        in EntityCommandBuffer ecb, in int networkId)
    {
        ecb.AppendToBuffer(entity, new AnimationTriggerBufferElement
        {
            NetworkId = networkId,
            Parameter = name,
        });
    }

    // To be used to set a parameter in a job
    [BurstCompile]
    public static void AddTriggerCommandJob(in FixedString32Bytes name, in Entity entity, 
        in EntityCommandBuffer.ParallelWriter ecb, in int sortKey, in int networkId)
    {
        ecb.AppendToBuffer(sortKey, entity, new AnimationTriggerBufferElement
        {
            NetworkId = networkId,
            Parameter = name,
        });
    }
}
