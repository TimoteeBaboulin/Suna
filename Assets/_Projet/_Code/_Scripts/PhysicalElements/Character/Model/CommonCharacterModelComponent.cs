using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct CommonCharacterModelBonesName : IComponentData
{
    public FixedString64Bytes WeaponSlotName;
}

public class CommonCharacterModelBonesTransform : IComponentData
{
    public Transform WeaponSlotTransform;
}
