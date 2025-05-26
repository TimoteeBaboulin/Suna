using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ThirdPersonCharacterModelPrefab : IComponentData
{
    public GameObject CorpoModelPrefab;
    public GameObject NatifModelPrefab;

    public CharacterColliderBones CorpoColliderBones;
    public CharacterColliderBones NatifColliderBones;

    public ModelAnimatorData CorpoAnimatorData;
    public ModelAnimatorData NatifAnimatorData;

    public float3 DeltaPosition;
}

public class ThirdPersonCharacterModelReference : ICleanupComponentData
{
    public GameObject ModelGameObject;
    public ModelAnimatorData AnimatorData;
    public float3 DeltaPosition;
}

public class ThirdPersonCharacterModelBonesTransform : IComponentData
{
    public Transform HeadBoneTransform;
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

