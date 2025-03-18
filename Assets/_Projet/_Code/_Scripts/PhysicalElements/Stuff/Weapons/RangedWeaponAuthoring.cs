using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[GhostComponent]
public struct StuffInfos : ISharedComponentData, IEquatable<StuffInfos>
{
    [GhostField] public FixedString64Bytes name;
    //[GhostField] public Image UIImage;
    [GhostField] public StuffType type;
    [GhostField] public TeamSideType side;
    [GhostField] public float deploymentSpeed;
    [GhostField] public float storageSpeed;
    [GhostField] public int price;
    public bool Equals(StuffInfos other)
    {
        return name.Equals(other.name);
    }

    public override int GetHashCode()
    {
        return name.GetHashCode();
    }
}

[GhostComponent]
public struct RangedWeaponCommonData : ISharedComponentData
{
    [GhostField] public float2 recoil;

    [GhostField] public float range;
    [GhostField] public float firerate;
    [GhostField] public float spread;
    [GhostField] public float spreadAiming;
    [GhostField] public float coefSpray;
    [GhostField] public float coefSprayAiming;
    [GhostField] public float ergonomics;
    [GhostField] public float roundsPerMin;
    [GhostField] public float dmgFallOff;
    [GhostField] public float coefModifMoveSpeed;
    [GhostField] public float coefModifMoveSpeedAiming;
    [GhostField] public float reloadSpeed;
    [GhostField] public float fastReloadSpeed;
    [GhostField] public float knockbackForceOnKill;

    [GhostField] public int damage;
    [GhostField] public int nbMagazine;
    [GhostField] public int magazineCapacity;

    //public ScopeData scope;
    //public HandleData handle;
    //public CrossData cross;
    //public SilencerData silencer;
    //public MagazineData magazine;

    //public float thorax;
    //public float stomach;
    //public float legs_Arms;
    //public float head;

    //public GameObject ammoType;
}

//public class RangedWeaponDataRef : IComponentData
//{
//    public RangedWeaponData Value;
//}

[GhostComponent]
public struct StuffOwner : IComponentData
{
    [GhostField] public Entity Value;
}

public class RangedWeaponAuthoring : MonoBehaviour
{
    [Header("Weapon Data")]
    public RangedWeaponData commonData;

    public class Baker : Baker<RangedWeaponAuthoring>
    {
        public override void Bake(RangedWeaponAuthoring authoring)
        {
            RangedWeaponData data = authoring.commonData;
            DependsOn(data);

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddSharedComponent(entity, new StuffInfos
            {
                name = new FixedString64Bytes(data.entityName),
                price = data.price,
                type = data.type,
                side = data.side,
                deploymentSpeed = data.deploymentSpeed,
                storageSpeed = data.storageSpeed
            });

            AddSharedComponent(entity, new RangedWeaponCommonData
            {
                recoil = data.recoil,
                damage = data.damage,
                range = data.range,
                spread = data.spread,
                spreadAiming = data.spreadAiming,
                coefSpray = data.coefSpray,
                coefSprayAiming = data.coefSprayAiming,
                ergonomics = data.ergonomics,
                roundsPerMin = data.roundsPerMin,
                dmgFallOff = data.dmgFallOff,
                coefModifMoveSpeed = data.coefModifMoveSpeed,
                coefModifMoveSpeedAiming = data.coefModifMoveSpeedAiming,
                reloadSpeed = data.reloadSpeed,
                fastReloadSpeed = data.fastReloadSpeed,
                knockbackForceOnKill = data.knockbackForceOnKill,
                nbMagazine = data.nbMagazine,
                magazineCapacity = data.magazineCapacity,
            });

            AddComponent(entity, new RangedWeaponDynamicData
            {
                currentAmmo = data.magazineCapacity + 1, // 1 = bullet in chamber
                remainingAmmo = data.magazineCapacity * (data.nbMagazine - 1),
            });

            //AddComponentObject(entity, new RangedWeaponDataRef
            //{
            //    Value = data,
            //});

            AddComponent(entity, new StuffOwner());
            //AddComponent<IsActiveWeapon>(entity);
        }
    }
}

//public struct ModifiersComponent : IComponentData
//{
//    public Entity scope;
//    public Entity handle;
//    public Entity cross;
//    public Entity silencer;
//    public Entity magazine;
//}

//AddComponent(entity, new ModifiersComponent
//{
//    scope = d.scope != null ? GetEntity(d.scope.prefab, TransformUsageFlags.Dynamic) : default,
//    handle = d.handle != null ? GetEntity(d.handle.prefab, TransformUsageFlags.Dynamic) : default,
//    cross = d.cross != null ? GetEntity(d.cross.prefab, TransformUsageFlags.Dynamic) : default,
//    silencer = d.silencer != null ? GetEntity(d.silencer.prefab, TransformUsageFlags.Dynamic) : default,
//    magazine = d.magazine != null ? GetEntity(d.magazine.prefab, TransformUsageFlags.Dynamic) : default,
//});

//AddComponent(entity, new StuffInfosComponent
//{
//    entityName = d.entityName,
//    price = d.price,
//    type = d.type,
//    side = d.side,
//    deploymentSpeed = d.deploymentSpeed,
//    storageSpeed = d.storageSpeed
//});

//AddComponent(entity, new RangedWeaponComponent
//{
//    recoil = d.recoil,
//    damage = d.damage,
//    range = d.range,
//    spread = d.spread,
//    spreadAiming = d.spreadAiming,
//    coefSpray = d.coefSpray,
//    coefSprayAiming = d.coefSprayAiming,
//    ergonomics = d.ergonomics,
//    roundsPerMin = d.roundsPerMin,
//    dmgFallOff = d.dmgFallOff,
//    coefModifMoveSpeed = d.coefModifMoveSpeed,
//    coefModifMoveSpeedAiming = d.coefModifMoveSpeedAiming,
//    reloadSpeed = d.reloadSpeed,
//    fastReloadSpeed = d.fastReloadSpeed,
//    knockbackForceOnKill = d.knockbackForceOnKill,
//    nbMagazine = d.nbMagazine,
//    magazineCapacity = d.magazineCapacity,

//    thorax = d.thorax,
//    stomach = d.stomach,
//    legs_Arms = d.stomach,
//    head = d.head
//});