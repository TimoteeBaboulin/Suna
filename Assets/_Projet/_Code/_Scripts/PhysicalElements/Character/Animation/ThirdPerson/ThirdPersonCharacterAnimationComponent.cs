using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class ThirdPersonCharacterAnimatorReference : IComponentData
{
    public Animator Animator;
}

[GhostComponent]
public struct ThirdPersonCharacterAnimationState : IComponentData
{
    [GhostField] public bool IsWalking;
}
