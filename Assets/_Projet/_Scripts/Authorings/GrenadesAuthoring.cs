using Unity.Entities;
using UnityEngine;

public class GrenadesAuthoring : MonoBehaviour
{
    [Header("Warning : this field must only be used on an \nobject present in the scene or an unique object !")]
    public GrenadeData grenade;

    public class Baker : Baker<GrenadesAuthoring>
    {
        public override void Bake(GrenadesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new GrenadeDatabaseAccess());

            AddComponent(entity, new StuffDatabaseAccess
            {
                IsConnectedToDatabase = false,
                NameInDatabase = authoring.grenade != null ? authoring.grenade.entityName : ""
            });
            AddComponent(entity, new StuffOwner());

            AddComponent<IsStuffInHand>(entity);
            SetComponentEnabled<IsStuffInHand>(entity, false);

            AddComponent<StuffProcessPending>(entity);
            SetComponentEnabled<StuffProcessPending>(entity, true);
        }
    }
}
