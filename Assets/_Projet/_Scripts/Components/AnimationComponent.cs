using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class AnimatorReference : IComponentData
{
    public Animator Animator;
}

[GhostEnabledBit]
public struct AnimationNeadUpdate : IEnableableComponent, IComponentData { }

[GhostComponent]
public struct AnimationFloatBufferElement : IBufferElementData
{
    [GhostField] public FixedString32Bytes Parameter;
    [GhostField] public float Value;
}

[GhostComponent]
public struct AnimationIntBufferElement : IBufferElementData
{
    [GhostField] public FixedString32Bytes Parameter;
    [GhostField] public int Value;
}

[GhostComponent]
public struct AnimationBoolBufferElement : IBufferElementData
{
    [GhostField] public FixedString32Bytes Parameter;
    [GhostField] public bool Value;
}

[GhostComponent]
public struct AnimationTriggerBufferElement : IBufferElementData
{
    [GhostField] public FixedString32Bytes Parameter;
}

