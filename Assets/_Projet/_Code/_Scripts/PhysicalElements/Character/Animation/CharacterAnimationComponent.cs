using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public class CharacterGameObjectPrefab : IComponentData
{
    public GameObject GameObjectPrefab;
    public float3 DeltaPosition;
    public string HeadBoneName;
    public string ViewBoneName;
}

public class CharacterAnimatorReference : ICleanupComponentData
{
    public Animator Animator;
    public float3 DeltaPosition;
}

public class CharacterModelBones : IComponentData
{
    public Transform HeadBoneTransform;
    public Transform ViewBoneTransform;
}

[GhostComponent]
public struct CharacterAnimationState : IComponentData
{
    [GhostField] public bool IsWalking;
}
