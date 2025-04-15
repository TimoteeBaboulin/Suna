using Unity.Entities;
using UnityEngine;

public class MeleeWeaponAuthoring : MonoBehaviour
{
    [Header("Warning : this field must only be used on an \nobject present in the scene or an unique object !")]
    public RangedWeaponData meleeWeapon;

    public class Baker : Baker<MeleeWeaponAuthoring>
    {
        public override void Bake(MeleeWeaponAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new MeleeWeaponDynamicData());
            AddComponent(entity, new MeleeWeaponDatabaseAccess());

            AddComponent(entity, new StuffDatabaseAccess
            {
                IsConnectedToDatabase = false,
                NameInDatabase = authoring.meleeWeapon != null ? authoring.meleeWeapon.entityName : ""
            });
            AddComponent(entity, new StuffDynamicData());

            AddComponent<IsStuffInHand>(entity);
            SetComponentEnabled<IsStuffInHand>(entity, false);

            AddComponent<StuffProcessPending>(entity);
            SetComponentEnabled<StuffProcessPending>(entity, true);
        }
    }
}

