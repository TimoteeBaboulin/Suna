using Unity.Entities;
using UnityEngine;

public class RangedWeaponAuthoring : MonoBehaviour
{
    [Header("Warning : this field must only be used on an \nobject present in the scene or an unique object !")]
    public RangedWeaponData rangedWeapon;

    public class Baker : Baker<RangedWeaponAuthoring>
    {
        public override void Bake(RangedWeaponAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new RangedWeaponDynamicData());
            AddComponent(entity, new RangedWeaponDatabaseAccess());

            AddComponent(entity, new StuffDatabaseAccess
            {
                IsConnectedToDatabase = false,
                NameInDatabase = authoring.rangedWeapon != null ? authoring.rangedWeapon.entityName : ""
            });
            AddComponent(entity, new StuffOwner());

            AddComponent<IsStuffInHand>(entity);
            SetComponentEnabled<IsStuffInHand>(entity, false);

            AddComponent<StuffProcessPending>(entity);
            SetComponentEnabled<StuffProcessPending>(entity, true);
        }
    }
}

