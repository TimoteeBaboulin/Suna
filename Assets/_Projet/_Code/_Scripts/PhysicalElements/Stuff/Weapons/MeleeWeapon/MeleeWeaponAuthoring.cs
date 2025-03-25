using Unity.Entities.Serialization;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;

using MeleeWeapon;

public class MeleeWeaponAuthoring : MonoBehaviour
{
    [Header("Weapon Data")]
    public MeleeWeaponData commonData;

    public class Baker : Baker<MeleeWeaponAuthoring>
    {
        public override void Bake(MeleeWeaponAuthoring authoring)
        {
            MeleeWeaponData data = authoring.commonData;
            DependsOn(data);

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddSharedComponent(entity, new StuffCommonData
            {
                name = new FixedString64Bytes(data.entityName),
                price = data.price,
                type = data.type,
                side = data.side,
                deploymentSpeed = data.deploymentSpeed,
                storageSpeed = data.storageSpeed,
                _stuffLocalOffsetView = data._stuffLocalOffsetView //temp
            });

            AddSharedComponent(entity, new CommonData
            {
                damage = data.damage,
                strongBlowDmg = data.strongBlowDmg,
                backStabDmg = data.backStabDmg,
                range = data.range,
                strikeRate = data.strikeRate,
                strongStrikeRate = data.strongStrikeRate
    });

            AddComponent(entity, new DynamicData());
            AddComponent(entity, new StuffOwner());

            AddComponent<IsStuffInHand>(entity);
            SetComponentEnabled<IsStuffInHand>(entity, false);

            AddComponentObject(entity, new StuffGameObjectPrefab
            {
                Value = data.gameobjectPrefab,
            });

            AddComponentObject(entity, new StuffUiImage
            {
                Value = data.UIImage,
            });
        }
    }
}
