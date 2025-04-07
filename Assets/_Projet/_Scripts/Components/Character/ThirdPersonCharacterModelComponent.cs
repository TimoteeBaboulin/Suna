using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ThirdPersonCharacterModelPrefab : IComponentData
{
    public GameObject CorpoModelPrefab;
    public float3 DeltaPosition;
}

public class ThirdPersonCharacterModelReference : ICleanupComponentData
{
    public GameObject ModelGameObject;
    public float3 DeltaPosition;
}

public struct ThirdPersonCharacterModelBonesName :  IComponentData
{
    public FixedString64Bytes ViewBoneName;
    public FixedString64Bytes HeadBoneName;
    public FixedString64Bytes ArmLeftBoneName0;
    public FixedString64Bytes ArmLeftBoneName1;
    public FixedString64Bytes ArmLeftBoneName2;
    public FixedString64Bytes ArmRightBoneName0;
    public FixedString64Bytes ArmRightBoneName1;
    public FixedString64Bytes ArmRightBoneName2;
    public FixedString64Bytes ThoraxBoneName;
    public FixedString64Bytes StomachBoneName0;
    public FixedString64Bytes StomachBoneName1;
    public FixedString64Bytes LegLeftBoneName0;
    public FixedString64Bytes LegLeftBoneName1;
    public FixedString64Bytes LegLeftBoneName2;
    public FixedString64Bytes LegRightBoneName0;
    public FixedString64Bytes LegRightBoneName1;
    public FixedString64Bytes LegRightBoneName2;
}

public class ThirdPersonCharacterModelBonesTransform : IComponentData
{
    public Transform ViewBoneTransform;
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

