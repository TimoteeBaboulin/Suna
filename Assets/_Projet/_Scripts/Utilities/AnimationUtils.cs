using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class AnimationUtils
{
    public static void UpdateFloatParameter(in Animator animator, DynamicBuffer<AnimationFloatBufferElement> floatBuffer)
    {
        foreach (var floatElement in floatBuffer)
        {
            animator.SetFloat(floatElement.Parameter.ToString(), floatElement.Value);
        }

        floatBuffer.Clear();
    }

    public static void UpdateIntParameter(in Animator animator, DynamicBuffer<AnimationIntBufferElement> intBuffer)
    {
        foreach (var intElement in intBuffer)
        {
            animator.SetInteger(intElement.Parameter.ToString(), intElement.Value);
        }

        intBuffer.Clear();
    }

    public static void UpdateBoolParameter(in Animator animator, DynamicBuffer<AnimationBoolBufferElement> boolBuffer)
    {
        foreach (var boolElement in boolBuffer)
        {
            animator.SetBool(boolElement.Parameter.ToString(), boolElement.Value);
        }

        boolBuffer.Clear();
    }

    public static void UpdateTriggerParameter(in Animator animator, DynamicBuffer<AnimationTriggerBufferElement> triggerBuffer)
    {
        foreach (var triggerElement in triggerBuffer)
        {
            animator.SetTrigger(triggerElement.Parameter.ToString());
        }

        triggerBuffer.Clear();
    }

    [BurstCompile]
    public static void AddFloatCommand(in FixedString32Bytes name, in float value, in Entity entity, in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        if (entityManager.HasComponent<AnimationFloatBufferElement>(entity))
        {
            ecb.AppendToBuffer(entity, new AnimationFloatBufferElement
            {
                Parameter = name,
                Value = value,
            });
        }
    }

    [BurstCompile]
    public static void AddIntCommand(in FixedString32Bytes name, in int value, in Entity entity, in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        if (entityManager.HasComponent<AnimationIntBufferElement>(entity))
        {
            ecb.AppendToBuffer(entity, new AnimationIntBufferElement
            {
                Parameter = name,
                Value = value,
            });
        }
    }

    [BurstCompile]
    public static void AddBoolCommand(in FixedString32Bytes name, in bool value, in Entity entity, in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        if (entityManager.HasComponent<AnimationBoolBufferElement>(entity))
        {
            ecb.AppendToBuffer(entity, new AnimationBoolBufferElement
            {
                Parameter = name,
                Value = value,
            });
        }
    }

    [BurstCompile]
    public static void AddTriggerCommand(in FixedString32Bytes name, in Entity entity, in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        if (entityManager.HasComponent<AnimationTriggerBufferElement>(entity))
        {
            ecb.AppendToBuffer(entity, new AnimationTriggerBufferElement
            {
                Parameter = name,
            });
        }
    }
}
