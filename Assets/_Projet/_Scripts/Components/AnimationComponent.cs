using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class AnimatorReference : IComponentData
{
    public Animator Animator;
}

public struct AnimationFloatBufferElement : IBufferElementData
{
    public FixedString32Bytes Parameter;
    public float Value;
}

public struct AnimationIntBufferElement : IBufferElementData
{
    public FixedString32Bytes Parameter;
    public int Value;
}

public struct AnimationBoolBufferElement : IBufferElementData
{
    public FixedString32Bytes Parameter;
    public bool Value;
}

public struct AnimationTriggerBufferElement : IBufferElementData
{
    public FixedString32Bytes Parameter;
}

