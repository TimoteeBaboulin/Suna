using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

public enum StuffSlot
{
    MainWeapon,
    SecondaryWeapon,
    Melee,
    Harvester,
    SpecialStuff1,
    SpecialStuff2,
    SpecialStuff3,
    SpecialStuff4,

    nbSlots
}

public enum StuffType
{
    RangedWeapon,
    MeleeWeapon,
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
public struct StuffEntityInHandRef : IComponentData
{
    [GhostField] public Entity Value;
}

[GhostComponent]
public struct StuffDynamicData : IComponentData
{
    [GhostField] public Entity owner;
    [GhostField] public Entity dropedEntityPrefab;
}

[GhostEnabledBit]
public struct IsStuffInHand : IComponentData, IEnableableComponent
{
}

[GhostEnabledBit]
[GhostComponent]
public struct StuffProcessPending : IComponentData, IEnableableComponent
{
    [GhostField] public Entity Owner;
    [GhostField] public float3 Position;
}

public struct StuffCommonData
{
    public BlobString Name;
    //public UnityObjectRef<GameObject> viewPrefab;
    //public UnityObjectRef<GameObject> UIPrefab;
    public StuffSlot slot;
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

//[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]

//public struct Command : ICommandData
//{
//    public float3 position;
//    public float3 normal;
//}


