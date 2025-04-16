using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct GameResourcesStuffEntityPrefabs : IComponentData
{
    public Entity rangedWeaponEntityPrefab;
    public Entity meleeWeaponEntityPrefab;
    public Entity grenadesEntityPrefab;
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
    public BlobArray<GrenadeCommonData> GrenadesCommonData;
    public BlobArray<MeleeWeaponCommonData> MeleeWeaponsCommonData;
}

public struct GameResourcesInstanciateStuffQueu : IBufferElementData
{
    public FixedString128Bytes StuffName;
    public Entity Owner;
}
