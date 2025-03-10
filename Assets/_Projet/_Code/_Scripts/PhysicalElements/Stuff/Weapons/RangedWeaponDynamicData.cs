using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[GhostComponent]
public struct RangedWeaponDynamicData : IComponentData
{
    [GhostField] public float reloadTimer;
    [GhostField] public float fastReloadTimer;
    [GhostField] public float firerateTimer;

    [GhostField] public int currentAmmo;
    [GhostField] public int remainingAmmo;
}