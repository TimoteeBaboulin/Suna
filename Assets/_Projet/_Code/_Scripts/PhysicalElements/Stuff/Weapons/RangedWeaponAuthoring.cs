using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

public struct ModifiersComponent : IComponentData
{
    public EntityPrefabReference scope;
    public EntityPrefabReference handle;
    public EntityPrefabReference cross;
    public EntityPrefabReference silencer;
    public EntityPrefabReference magazine;
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

            AddComponent(entity, new StuffInfosComponent
            {
                entityName = d.entityName,
                price = d.price,
                type = d.type,
                side = d.side,
                deploymentSpeed = d.deploymentSpeed,
                storageSpeed = d.storageSpeed
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

                thorax = d.thorax,
                stomach = d.stomach,
                legs_Arms = d.stomach,
                head = d.head
            });

            AddComponent(entity, new ModifiersComponent
            {

                scope = d.scope != null ? new EntityPrefabReference(d.scope.prefab) : default,
                handle = d.handle != null ? new EntityPrefabReference(d.handle.prefab) : default,
                cross = d.cross != null ? new EntityPrefabReference(d.cross.prefab) : default,
                silencer = d.silencer != null ? new EntityPrefabReference(d.silencer.prefab) : default,
                magazine = d.magazine != null ? new EntityPrefabReference(d.magazine.prefab) : default,
            });
        }
    }
}
