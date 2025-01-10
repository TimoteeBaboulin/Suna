using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

public struct PrefabReferenceComponent : IComponentData
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
    public EntityPrefabReference scope;
    public EntityPrefabReference handle;
    public EntityPrefabReference cross;
    public EntityPrefabReference silencer;
    public EntityPrefabReference magazine;
}

public struct ModifierPrefabComponent : IComponentData
{
    public Entity ModifierPrefab;
}

public struct OverrideCorpsDmgComponent : IComponentData
{
    public float thorax;
    public float stomach;
    public float legs_Arms;
    public float head;
}

public class RangedWeaponAuthoring : MonoBehaviour
{
    [Header("Weapon Data")]
    public RangedWeaponData commonData;

    public class Baker : Baker<RangedWeaponAuthoring>
    {
        public override void Bake(RangedWeaponAuthoring authoring)
        {
            RangedWeaponData d = authoring.commonData;
            DependsOn(d);

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PrefabReferenceComponent
            {
                prefab = new EntityPrefabReference(d.prefab)
            });

            AddComponent(entity, new RangedWeaponComponent
            {
                recoil = d.recoil,
                damage = d.damage,
                range = d.range,
                spread = d.spread,
                spreadAiming = d.spreadAiming,
                coefSpray = d.coefSpray,
                coefSprayAiming = d.coefSprayAiming,
                ergonomics = d.ergonomics,
                roundsPerMin = d.roundsPerMin,
                dmgFallOff = d.dmgFallOff,
                coefModifMoveSpeed = d.coefModifMoveSpeed,
                coefModifMoveSpeedAiming = d.coefModifMoveSpeedAiming,
                reloadSpeed = d.reloadSpeed,
                fastReloadSpeed = d.fastReloadSpeed,
                knockbackForceOnKill = d.knockbackForceOnKill,
                nbMagazine = d.nbMagazine,
                magazineCapacity = d.magazineCapacity,
            });

            //Warning : Some Modifiers prefabs can be null
            AddComponent(entity, new ModifiersComponent
            {
                //scope = d.scope.prefab != null ? GetEntity(d.scope.prefab, TransformUsageFlags.None) : Entity.Null,
                //handle = d.handle.prefab != null ? GetEntity(d.handle.prefab, TransformUsageFlags.None) : Entity.Null,
                //cross = d.cross.prefab != null ? GetEntity(d.cross.prefab, TransformUsageFlags.None) : Entity.Null,
                //silencer = d.silencer.prefab != null ? GetEntity(d.silencer.prefab, TransformUsageFlags.None) : Entity.Null,
                //magazine = d.magazine.prefab != null ? GetEntity(d.magazine.prefab, TransformUsageFlags.None) : Entity.Null,

                //scope = new EntityPrefabReference(d.scope.prefab != null ? d.scope.prefab : null),
                //handle = new EntityPrefabReference(d.handle.prefab != null ? d.handle.prefab : null),
                //cross = new EntityPrefabReference(d.cross.prefab != null ? d.cross.prefab : null),
                //silencer = new EntityPrefabReference(d.silencer.prefab != null ? d.silencer.prefab : null),
                //magazine = new EntityPrefabReference(d.magazine.prefab != null ? d.magazine.prefab : null),
            });

            AddComponent(entity, new OverrideCorpsDmgComponent
            {
                thorax = d.thorax,
                stomach = d.stomach,
                legs_Arms = d.stomach,
                head = d.head
            });

            //Component
        }
    }
}
