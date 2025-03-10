using Unity.Entities;
using UnityEngine;

public struct RangedWeaponDynamicData : IComponentData
{
    public float reloadTimer;
    public float fastReloadTimer;
    public float firerateTimer;
    public int ammo;
}