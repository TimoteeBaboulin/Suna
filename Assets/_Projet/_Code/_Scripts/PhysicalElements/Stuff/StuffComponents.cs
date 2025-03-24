using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

public enum StuffType
{
    MainWeapon,
    SecondaryWeapon,
    Melee,
    Harvester,
    SpecialStuff
}

[GhostComponent]
public struct StuffCommonData : ISharedComponentData, IEquatable<StuffCommonData>
{
    [GhostField] public FixedString64Bytes name;
    [GhostField] public StuffType type;
    [GhostField] public TeamSideType side;
    [GhostField] public float deploymentSpeed;
    [GhostField] public float storageSpeed;
    [GhostField] public int price;

    public Vector3 _stuffLocalOffsetView; //temp

    public bool Equals(StuffCommonData other)
    {
        return name.Equals(other.name);
    }

    public override int GetHashCode()
    {
        return name.GetHashCode();
    }
}

[GhostComponent]
public struct StuffOwner : IComponentData
{
    [GhostField] public Entity Value;
}

[GhostEnabledBit]
public struct IsStuffInHand : IComponentData, IEnableableComponent 
{ 
}

public class StuffGameObjectPrefab : IComponentData
{
    public GameObject Value;
}

public class StuffGameObjectRef : ICleanupComponentData
{
    public GameObject Value;
}

public class StuffUiImage : ICleanupComponentData
{
    public Image Value;
}


