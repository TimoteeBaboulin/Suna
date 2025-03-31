using Unity.Collections;
using Unity.Entities;

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

public struct GameResourcesInstanciateStuffQueu : IBufferElementData
{
    public FixedString128Bytes StuffName;
    public Entity Owner;
}
