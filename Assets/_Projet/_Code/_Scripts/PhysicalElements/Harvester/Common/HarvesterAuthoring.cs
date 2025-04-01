using Unity.Entities;
using UnityEngine;

public class HarvesterAuthoring : MonoBehaviour
{
    [Header("Warning : this field must only be used on an \nobject present in the scene or an unique object !")]
    public HarvesterData harvester;

    public class Baker : Baker<HarvesterAuthoring>
    {
        public override void Bake(HarvesterAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new StuffDatabaseAccess
            {
                IsConnectedToDatabase = false,
                NameInDatabase = authoring.harvester != null ? authoring.harvester.entityName : ""
            });
            AddComponent(entity, new StuffOwner());

            AddComponent<IsStuffInHand>(entity);
            SetComponentEnabled<IsStuffInHand>(entity, false);

            AddComponent(entity, new HarvesterComponent
            {
                defuseRange = authoring.harvester.defuseRange,
                pickupDistance = authoring.harvester.pickupDistance,

                IsActive = false
            });

            AddComponent<StuffProcessPending>(entity);
            SetComponentEnabled<StuffProcessPending>(entity, true);

            AddComponent<HarvesterPlanting>(entity);
            SetComponentEnabled<HarvesterPlanting>(entity, false);

            AddComponent<HarvesterPlanted>(entity);
            SetComponentEnabled<HarvesterPlanted>(entity, false);

            //TODO: Set the tag at the start of the match instead of at the loading of the map
            AddComponent<HarvesterRespawn>(entity);
        }
    }
}
