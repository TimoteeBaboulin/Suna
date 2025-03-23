using Unity.Collections;
using Unity.Entities;
using UnityEngine;

using RangedWeapon;

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

            AddSharedComponent(entity, new StuffCommonData
            {
                name = new FixedString64Bytes(data.entityName),
                price = data.price,
                type = data.type,
                side = data.side,
                deploymentSpeed = data.deploymentSpeed,
                storageSpeed = data.storageSpeed
            });

            AddSharedComponent(entity, new CommonData
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

            AddComponent(entity, new DynamicData
            {
                currentAmmo = data.magazineCapacity + 1, // 1 = bullet in chamber
                remainingAmmo = data.magazineCapacity * (data.nbMagazine - 1),
            });

            AddComponent(entity, new StuffOwner());

            AddComponent<IsStuffInHand>(entity);
            SetComponentEnabled<IsStuffInHand>(entity, false);

            AddComponentObject(entity, new StuffPrefab
            {
                Value = data.prefab,
            });

            AddComponentObject(entity, new StuffAnimatorRef
            {
                Animator = GetComponent<Animator>(),
            });

            AddComponentObject(entity, new StuffUiImage
            {
                Value = data.UIImage,
            });
        }
    }
}
