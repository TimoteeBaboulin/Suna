using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[GhostComponent]
public struct StuffEntityPrefabsBuffer : IBufferElementData
{
    [GhostField] public Entity dropedEntityPrefab;
    [GhostField] public Entity inHandEntityPrefab;
    [GhostField] public Entity thrownGrenadeEntityPrefab; //Only useful for grenades but I didn't have time to refactor this, sorry
}

public struct GameResourcesDatabase : IComponentData
{
    public BlobAssetReference<StuffDatabase> StuffDatabaseRef;
}

public struct StuffDatabase
{
    public BlobArray<StuffCommonData> StuffCommonData;
    public BlobArray<RangedWeaponCommonData> RangedWeaponsCommonData;
    public BlobArray<GrenadeCommonData> GrenadesCommonData;
    public BlobArray<MeleeWeaponCommonData> MeleeWeaponsCommonData;
}

public struct InstantiateStuffQueue : IBufferElementData
{
    public FixedString128Bytes StuffName;
    public Entity Owner;
    public float3 Position;
}

public class GameResourcesViewPrefabs : IComponentData
{
    public List<GameObject> List_;
    public List<GameObject> List_Baked;

    public List<GameObject> GetPrefabsList(TeamSideType side)
    {
        switch (side)
        {
            case TeamSideType.Corpo:
                return List_Baked;
            case TeamSideType.Natif:
                return List_;
            case TeamSideType.Neutre:
                return null;
            default:
                return null;
        }
    }
}
