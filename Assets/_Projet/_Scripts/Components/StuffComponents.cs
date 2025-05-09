using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using static ak.wwise;

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

public enum StuffState
{
    InShop,
    Instantiate,
    Equip,
    Unequip,
    Throw,
    Drop,
    Plant
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
    [GhostField] public StuffState state;
    [GhostField] public Entity owner;
    [GhostField] public Entity dropedEntityPrefab;
    [GhostField] public Entity dropedEntityRef;
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
    public uint killGain;

    public bool canADS;
    public float ADSFOV;

    public int dataID;

    public float3 _stuffLocalOffsetView;
    public float3 _stuffLocalOffsetView_Baked;

    public float3 GetStuffLocalOffsetView(TeamSideType side)
    {
        switch (side)
        {
            case TeamSideType.Corpo:
                return _stuffLocalOffsetView_Baked;
            case TeamSideType.Natif:
                return _stuffLocalOffsetView;
            case TeamSideType.Neutre:
                break;
            default:
                break;
        }
        return default;
    }
}

public class StuffGameObjectRef : ICleanupComponentData
{
    public GameObject View;
    public GameObject View_Baked_;

    public StuffGameObjectRef Instantiate(GameResourcesViewPrefabs grViewPrefabs, RefRO<StuffDatabaseAccess> stuffDataRef)
    {
        StuffGameObjectRef goRef = new StuffGameObjectRef();

        if (grViewPrefabs.List_[stuffDataRef.ValueRO.ID] != null)
        {
            View = GameObject.Instantiate(grViewPrefabs.List_[stuffDataRef.ValueRO.ID]);
            View.name = grViewPrefabs.List_[stuffDataRef.ValueRO.ID].name;
        }

        if (grViewPrefabs.List_Baked[stuffDataRef.ValueRO.ID] != null)
        {
            View_Baked_ = GameObject.Instantiate(grViewPrefabs.List_Baked[stuffDataRef.ValueRO.ID]);
            View_Baked_.name = grViewPrefabs.List_Baked[stuffDataRef.ValueRO.ID].name + "_Baked";
        }

        return goRef;
    }

    public GameObject GetGameObjectSide(TeamSideType side)
    {
        switch (side)
        {
            case TeamSideType.Corpo:
                return View_Baked_;
            case TeamSideType.Natif:
                return View;
            case TeamSideType.Neutre:
                return null;
            default:
                return null;
        }
    }

    public Transform GetTransformSide(TeamSideType side)
    {
        switch (side)
        {
            case TeamSideType.Corpo:
                return View_Baked_ != null ? View_Baked_.transform : null;
            case TeamSideType.Natif:
                return View != null ? View.transform : null;
            case TeamSideType.Neutre:
                return null;
            default:
                return null;
        }
    }

    public void SwitchSetActiveSide(TeamSideType side, bool isActive)
    {
        switch (side)
        {
            case TeamSideType.Corpo:
                if (View_Baked_ != null) View_Baked_.SetActive(isActive);
                if (View != null) View.SetActive(!isActive);
                break;
            case TeamSideType.Natif:
                if (View_Baked_ != null) View_Baked_.SetActive(!isActive);
                if (View != null) View.SetActive(isActive);
                break;
            case TeamSideType.Neutre:
                break;
            default:
                break;

        }
    }

    public void SetActiveOne(bool isActive)
    {
        if (View_Baked_ != null)
        {
            View_Baked_.SetActive(isActive);
            if (View != null) View.SetActive(false);
        }
        else if (View != null)
        {
            View.SetActive(isActive);
            if (View_Baked_ != null) View_Baked_.SetActive(false);
        }
    }

    public void SetParent(Transform parent)
    {
        Debug.Log("SetParent " + parent.name + " for " + View.name);
        if (View_Baked_ != null) View_Baked_.transform.SetParent(parent);
        if (View != null) View.transform.SetParent(parent);
    }

    public void SetTransform(Transform transform)
    {
        if (View_Baked_ != null)
        {
            View_Baked_.transform.position = transform.position;
            View_Baked_.transform.rotation = transform.rotation;
        }

        if (View != null)
        {
            View.transform.position = transform.position;
            View.transform.rotation = transform.rotation;
        }
    }

    public void SetTransform(LocalTransform transform)
    {
        if (View_Baked_ != null)
        {
            View_Baked_.transform.position = transform.Position;
            View_Baked_.transform.rotation = transform.Rotation;
        }

        if (View != null)
        {
            View.transform.position = transform.Position;
            View.transform.rotation = transform.Rotation;
        }
    }

    public Transform GetTransform()
    {
        if (View_Baked_ != null) return View_Baked_.transform;
        if (View != null) return View.transform;
        return null;
    }

    public void Destroy()
    {
        if (View != null)
        {
            GameObject.Destroy(View);
        }
        if (View_Baked_ != null)
        {
            GameObject.Destroy(View_Baked_);
        }
    }
}


