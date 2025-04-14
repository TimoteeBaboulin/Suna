using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[GhostComponent]
public struct StuffEntityPrefabsBuffer : IBufferElementData
{
    [GhostField] public Entity Value;
}

public struct GameResourcesDatabase : IComponentData
{
    public BlobAssetReference<StuffDatabase> StuffDatabaseRef;
}

public struct StuffDatabase
{
    public BlobArray<StuffCommonData> StuffCommonData;
    public BlobArray<RangedWeaponCommonData> RangedWeaponsCommonData;
    public BlobArray<MeleeWeaponCommonData> MeleeWeaponsCommonData;
}

public struct GameResourcesInstantiateStuffQueue : IBufferElementData
{
    public FixedString128Bytes StuffName;
    public Entity Owner;
    //public float3 Position;
}

//public class GameResourcesStuffViewPrefabBuffer : IBufferElementData
//{
//    public GameObject Value;
//}
