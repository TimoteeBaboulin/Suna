using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

public enum StuffInventoryLocation
{
    MainWeapon,
    SecondaryWeapon,
    Melee,
    Harvester,
    Grenade,
    SpecialStuff1,
    SpecialStuff2,
    SpecialStuff3,
    SpecialStuff4,

    nbLocation
}

public enum StuffType
{
    RangedWeapon,
    MeleeWeapon,
    Grenade,
    Harvester,
}

[GhostComponent]
public struct StuffDatabaseAccess : IComponentData
{
    [GhostField] public int ID;
    [GhostField] public bool IsConnectedToDatabase;
    [GhostField] public FixedString128Bytes NameInDatabase;

    public ref StuffCommonData GetData(ref GameResourcesDatabase database)
    {
        return ref database.StuffDatabaseRef.Value.StuffCommonData[ID];
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


public struct StuffProcessPending : IComponentData, IEnableableComponent
{
}

public struct StuffCommonData
{
    public BlobString Name;
    public UnityObjectRef<GameObject> viewPrefab;
    //public UnityObjectRef<GameObject> UIPrefab;
    public StuffInventoryLocation location;
    public StuffType type;
    public TeamSideType side;
    public float deploymentSpeed;
    public float storageSpeed;
    public int price;
    public float3 _stuffLocalOffsetView;
    public uint killGain;

    public bool canADS;
    public float ADSFOV;

    public int dataID;
}

//public class StuffGameObjectViewPrefab : IComponentData
//{
//    public GameObject Value;
//}

public class StuffGameObjectRef : ICleanupComponentData
{
    public GameObject Value;
}

//public class StuffUiImage : ICleanupComponentData
//{
//    public Image Value;
//}


