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

            //Warning : Some Modifiers prefabs can be null
            AddComponent(entity, new ModifiersComponent
            {
                //scope = new EntityPrefabReference(d.scope.prefab != null ? d.scope.prefab : null),
                //handle = new EntityPrefabReference(d.handle.prefab != null ? d.handle.prefab : null),
                //cross = new EntityPrefabReference(d.cross.prefab != null ? d.cross.prefab : null),
                //silencer = new EntityPrefabReference(d.silencer.prefab != null ? d.silencer.prefab : null),
                //magazine = new EntityPrefabReference(d.magazine.prefab != null ? d.magazine.prefab : null),
            });

            //scope = d.scope.prefab != null ? GetEntity(d.scope.prefab, TransformUsageFlags.None) : Entity.Null,
            //handle = d.handle.prefab != null ? GetEntity(d.handle.prefab, TransformUsageFlags.None) : Entity.Null,
            //cross = d.cross.prefab != null ? GetEntity(d.cross.prefab, TransformUsageFlags.None) : Entity.Null,
            //silencer = d.silencer.prefab != null ? GetEntity(d.silencer.prefab, TransformUsageFlags.None) : Entity.Null,
            //magazine = d.magazine.prefab != null ? GetEntity(d.magazine.prefab, TransformUsageFlags.None) : Entity.Null,

            //scope = d.scope.prefab != null ? new EntityPrefabReference(d.scope.prefab) : null,
            //handle = new EntityPrefabReference(d.handle.prefab != null ? d.handle.prefab : null),
            //cross = new EntityPrefabReference(d.cross.prefab != null ? d.cross.prefab : null),
            //silencer = new EntityPrefabReference(d.silencer.prefab != null ? d.silencer.prefab : null),
            //magazine = new EntityPrefabReference(d.magazine.prefab != null ? d.magazine.prefab : null),

            //Component
        }
    }
}
