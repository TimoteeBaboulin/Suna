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
    public string ArmLeftBoneName0;
    public string ArmLeftBoneName1;
    public string ArmLeftBoneName2;
    public string ArmRightBoneName0;
    public string ArmRightBoneName1;
    public string ArmRightBoneName2;
    public string ThoraxBoneName;
    public string StomachBoneName0;
    public string StomachBoneName1;
    public string LegLeftBoneName0;
    public string LegLeftBoneName1;
    public string LegLeftBoneName2;
    public string LegRightBoneName0;
    public string LegRightBoneName1;
    public string LegRightBoneName2;
}

public class CharacterAnimatorReference : ICleanupComponentData
{
    public Animator Animator;
    public CharacterModelScript CharacterModel;
    public float3 DeltaPosition;
}

public class CharacterModelBones : IComponentData
{
    public Transform HeadBoneTransform;
    public Transform ViewBoneTransform;
    public Transform ArmLeftBoneTransform0;
    public Transform ArmLeftBoneTransform1;
    public Transform ArmLeftBoneTransform2;
    public Transform ArmRightBoneTransform0;
    public Transform ArmRightBoneTransform1;
    public Transform ArmRightBoneTransform2;
    public Transform ThoraxBoneTransform;
    public Transform StomachBoneTransform0;
    public Transform StomachBoneTransform1;
    public Transform LegLeftBoneTransform0;
    public Transform LegLeftBoneTransform1;
    public Transform LegLeftBoneTransform2;
    public Transform LegRightBoneTransform0;
    public Transform LegRightBoneTransform1;
    public Transform LegRightBoneTransform2;
}

[GhostComponent]
public struct CharacterAnimationState : IComponentData
{
    [GhostField] public bool IsWalking;
}
