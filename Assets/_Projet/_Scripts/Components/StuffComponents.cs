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
    HEGrenade,
    Flashbang,

    nbSlots
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
public struct StuffEntityInHandRef : IComponentData
{
    [GhostField] public Entity Value;
}

[GhostComponent]
public struct StuffDynamicData : IComponentData
{
    [GhostField] public Entity owner;
    [GhostField] public Entity dropedEntityPrefab;
    [GhostField] public Entity grenadeThrownPrefab; //Only useful for grenades but I didn't have time to refactor this, sorry
}

[GhostEnabledBit]
[GhostComponent]
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

public class StuffGameObjectRef : ICleanupComponentData
{
    public GameObject Value;
}


