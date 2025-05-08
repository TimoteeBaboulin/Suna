using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class AnimatorReference : IComponentData
{
    public Animator Animator;
}

public struct AnimationFloatBufferElement : IBufferElementData
{
    public int NetworkId;
    public FixedString32Bytes Parameter;
    public float Value;
}

public struct AnimationIntBufferElement : IBufferElementData
{
    public int NetworkId;
    public FixedString32Bytes Parameter;
    public int Value;
}

public struct AnimationBoolBufferElement : IBufferElementData
{
    public int NetworkId;
    public FixedString32Bytes Parameter;
    public bool Value;
}

public struct AnimationTriggerBufferElement : IBufferElementData
{
    public int NetworkId;
    public FixedString32Bytes Parameter;
}

public struct FloatParameterRpc : IRpcCommand
{
    public int NetworkId;
    public FixedString32Bytes Parameter;
    public float Value;
}

public struct IntParameterRpc : IRpcCommand
{
    public int NetworkId;
    public FixedString32Bytes Parameter;
    public int Value;
}

public struct BoolParameterRpc : IRpcCommand
{
    public int NetworkId;
    public FixedString32Bytes Parameter;
    public bool Value;
}

public struct TriggerParameterRpc : IRpcCommand
{
    public int NetworkId;
    public FixedString32Bytes Parameter;
}
