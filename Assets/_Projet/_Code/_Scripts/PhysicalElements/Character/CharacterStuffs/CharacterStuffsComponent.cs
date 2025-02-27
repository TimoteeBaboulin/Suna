using System.Collections.Generic;
using Unity.Entities;

public struct CharacterStuffsPrefabComponent : IComponentData
{
    public Entity mainWeapon;
    public Entity secondaryWeapon;
    public Entity melee;
    //public Entity Harvester;
}

public struct CharacterStuffsComponent : IComponentData
{
    public Entity mainWeapon;
    public Entity secondaryWeapon;
    public Entity melee;
    //public Entity Harvester;
}

[InternalBufferCapacity(4)]
public struct ProjectileBuffer : IBufferElementData
{
    public Entity Value;
}

public struct CharacterCurrentStuffComponent : IComponentData
{
    public Entity Value;
}
