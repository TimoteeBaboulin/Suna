using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class CharacterAnimatorReference : IComponentData
{
    public Animator Animator;
}

[GhostComponent]
public struct CommonCharacterAnimationState : IComponentData
{
    [GhostField] public bool IsWalking;
}
