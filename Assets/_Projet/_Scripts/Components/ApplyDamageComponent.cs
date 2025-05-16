using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public enum DamageSource
{
    Invalid,
    Weapon,
    Grenade,
    Fall,
}

[GhostComponent]
[GhostEnabledBit]
public struct Damageable : IEnableableComponent, IComponentData { }

public struct ApplyDamage : IComponentData
{
    public DamageSource source;
    public Entity playerSource; //Nullable
    public Entity targetEntity;

    public Entity weapon;
    public Entity grenade;

    public uint killReward;

    public float damage;

    public float3 sourcePosition;
}

public struct ApplyDamageCommand : IComponentData
{
    public float3 position;
    public Entity target; //YOLO
}

public struct KillDamageCommand : IComponentData
{
    public ClientComponent target;
    public ClientComponent killer;
}