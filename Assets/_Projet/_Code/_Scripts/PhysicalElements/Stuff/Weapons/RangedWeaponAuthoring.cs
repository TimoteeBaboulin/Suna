using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.NetCode;
using UnityEngine;

public class RangedWeaponDataRef : IComponentData
{
    public RangedWeaponData Value;
}

[GhostComponent]
public struct WeaponOwner : IComponentData
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

            AddComponent(entity, new RangedWeaponDynamicData
            {
                currentAmmo = data.magazineCapacity + 1, // 1 = bullet in chamber
                remainingAmmo = data.magazineCapacity * (data.nbMagazine - 1),
            });

            AddComponentObject(entity, new RangedWeaponDataRef
            {
                Value = data,
            });

            AddComponent(entity, new WeaponOwner());
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