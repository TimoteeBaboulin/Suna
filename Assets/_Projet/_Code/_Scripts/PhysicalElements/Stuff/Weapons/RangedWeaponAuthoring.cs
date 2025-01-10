using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

public struct PrefabComponent : IComponentData
{
    public EntityPrefabReference prefab;
}

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

    public int nbMagazine;
    public int magazineCapacity;

    public int currentAmmo;
    public bool isReloading;
}

public struct ModifiersComponent : IComponentData
{
    public Entity scope;
    public Entity handle;
    public Entity cross;
    public Entity silencer;
    public Entity magazine;
}

public struct CorpsDmgComponent : IComponentData
{
    public float thorax;
    public float stomach;
    public float legs_Arms;
    public float head;
}

public class RangedWeaponAuthoring : MonoBehaviour
{
    [Header("Common Data")]
    public RangedWeaponData commonData;

    public class Baker : Baker<RangedWeaponAuthoring>
    {
        public override void Bake(RangedWeaponAuthoring authoring)
        {
            DependsOn(authoring.commonData);

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PrefabComponent
            {
                prefab = new EntityPrefabReference(authoring.commonData.prefab)
            });

            AddComponent(entity, new RangedWeaponComponent
            {
                recoil = authoring.commonData.recoil,
                damage = authoring.commonData.damage,
                range = authoring.commonData.range,
                spread = authoring.commonData.spread,
                spreadAiming = authoring.commonData.spreadAiming,
                coefSpray = authoring.commonData.coefSpray,
                coefSprayAiming = authoring.commonData.coefSprayAiming,
                ergonomics = authoring.commonData.ergonomics,
                roundsPerMin = authoring.commonData.roundsPerMin,
                dmgFallOff = authoring.commonData.dmgFallOff,
                coefModifMoveSpeed = authoring.commonData.coefModifMoveSpeed,
                coefModifMoveSpeedAiming = authoring.commonData.coefModifMoveSpeedAiming,
                reloadSpeed = authoring.commonData.reloadSpeed,
                fastReloadSpeed = authoring.commonData.fastReloadSpeed,
                knockbackForceOnKill = authoring.commonData.knockbackForceOnKill,
                nbMagazine = authoring.commonData.nbMagazine,
                magazineCapacity = authoring.commonData.magazineCapacity,
            });

            AddComponent(entity, new ModifiersComponent
            {
                //scope = authoring.commonData.scope,
                //handle = authoring.commonData.handle,
                //cross = authoring.commonData.cross,
                //silencer = authoring.commonData.silencer,
                //magazine = authoring.commonData.magazine

                //        if (authoring.commonData.scope != null)
                //scope = GetEntity(authoring.commonData.scope.prefab, TransformUsageFlags.Dynamic),
            });
        }
    }
}
