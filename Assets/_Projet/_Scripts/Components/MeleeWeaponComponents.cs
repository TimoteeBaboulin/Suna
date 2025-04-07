using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent]
public struct MeleeWeaponDynamicData : IComponentData
{
    [GhostField] public float strikeTimer;
    [GhostField] public float strongStrikeTimer;
}

public struct MeleeWeaponCommonData : IBufferElementData
{
    public float damage;
    public float strongBlowDmg;
    public float backStabDmg;
    public float range;
    public float strikeRate;
    public float strongStrikeRate;
}

[GhostComponent]
public struct MeleeWeaponDatabaseAccess : IComponentData
{
    [GhostField] public int Value;

    public ref MeleeWeaponCommonData GetData(ref GameResourcesDatabase database)
    {
        return ref database.StuffDatabaseRef.Value.MeleeWeaponsCommonData[Value];
    }
}