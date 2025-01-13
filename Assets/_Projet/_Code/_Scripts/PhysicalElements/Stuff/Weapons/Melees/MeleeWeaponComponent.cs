using Unity.Entities;
using UnityEngine;

public struct MeleeWeaponComponent : IComponentData
{
    public float damage;
    public float range;
    public float strongBlowDmg;
    public float backStabDmg;
    public float strikeRate;
}
