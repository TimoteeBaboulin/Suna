using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct GameResourcesStuffEntityPrefabs : IComponentData
{
    public Entity rangedWeaponEntityPrefab;
    public Entity meleeWeaponEntityPrefab;
    public Entity harvesterEntityPrefab;
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

public struct GameResourcesInstantiateStuffQueu : IBufferElementData
{
    public FixedString128Bytes StuffName;
    public Entity Owner;
}

//public class GameResourcesStuffViewPrefabBuffer : IBufferElementData
//{
//    public GameObject Value;
//}
