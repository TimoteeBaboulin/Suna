using Unity.Entities.Serialization;
using Unity.Entities;
using UnityEngine;

public class MeleeWeaponAuthoring : MonoBehaviour
{
    [Header("Weapon Data")]
    public MeleeWeaponData commonData;

    public class Baker : Baker<MeleeWeaponAuthoring>
    {
        public override void Bake(MeleeWeaponAuthoring authoring)
        {
            MeleeWeaponData d = authoring.commonData;
            DependsOn(d);

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PrefabReferenceComponent
            {
                prefab = GetEntity(d.prefab, TransformUsageFlags.Dynamic)
            });

            AddComponent(entity, new StuffInfosComponent
            {
                entityName = d.entityName,
                type = d.type,
                side = d.side,
                deploymentSpeed = d.deploymentSpeed,
                storageSpeed = d.storageSpeed
            });

            AddComponent(entity, new MeleeWeaponComponent
            {
                damage = d.damage,
                range = d.range,
                strongBlowDmg = d.strongBlowDmg,
                backStabDmg = d.backStabDmg,
                strikeRate = d.strikeRate
            });
        }
    }
}
