using Unity.Entities;
using UnityEngine;

public struct RangedWeaponComponent : IComponentData
{
    /// <summary>
    /// two scalars that can be used to control \nthe amplitude spray pattern functions
    /// </summary>
    public Vector2 recoil;

    /// <summary>
    ///in life points
    /// </summary>
    public float damage;

    /// <summary>
    /// in meter
    /// </summary>
    public float range;

    /// <summary>
    /// in minutes of angle
    /// </summary>
    public float spread;

    /// <summary>
    /// in minutes of angle
    /// </summary>
    public float spreadAiming;

    /// <summary>
    /// scalar which allows you to amplify or reduce \nthe effects of the spray pattern
    /// </summary>
    public float coefSpray;

    /// <summary>
    /// scalar which allows you to amplify or reduce \nthe effects of the spray pattern on aiming
    /// </summary>
    public float coefSprayAiming;

    /// <summary>
    /// aiming speed in milliseconds
    /// </summary>
    public float ergonomics;

    /// <summary>
    /// in rounds per minute
    /// </summary>
    public float roundsPerMin;

    /// <summary>
    /// in damage per meter
    /// </summary>
    public float dmgFallOff;

    /// <summary>
    /// The coefficient of modification \nof movement speed (scalar)
    /// </summary>
    public float coefModifMoveSpeed;

    /// <summary>
    /// The coefficient of modification \nof movement speed while aiming (scalar)
    /// </summary>
    public float coefModifMoveSpeedAiming;

    /// <summary>
    /// in seconde
    /// </summary>
    public float reloadSpeed;

    /// <summary>
    /// in seconde
    /// </summary>
    public float fastReloadSpeed;

    /// <summary>
    /// Propulsion of the enemy ragdoll when it dies
    /// </summary>
    public float knockbackForceOnKill;

    public float thorax;
    public float stomach;
    public float legs_Arms;
    public float head;

    public int nbMagazine;
    public int magazineCapacity;
    public int currentAmmo;

    public bool isReloading;
}