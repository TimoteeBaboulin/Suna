using Unity.Entities;
using UnityEngine;

public class HarvesterAuthoring : MonoBehaviour
{
    public GameObject harvesterPrefab;

    public class HarvesterBaker : Baker<HarvesterAuthoring>
    {
        public GameObject harvesterPrefab;

        public override void Bake(HarvesterAuthoring authoring)
        {
            HarvesterComponent harvester = new HarvesterComponent
            {
                Owner = Entity.Null,
                IsActive = false
            };
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, harvester);

            //Enableable Tags
            AddComponent<IsStuffInHand>(entity);
            SetComponentEnabled<IsStuffInHand>(entity, false);
            AddComponent<HarvesterPlanting>(entity);
            SetComponentEnabled<HarvesterPlanting>(entity, false);
            AddComponent<HarvesterPlanted>(entity);
            SetComponentEnabled<HarvesterPlanted>(entity, false);

            //TODO: Make the game wait before starting the rounds while people load
            AddComponent<HarvesterRespawn>(entity);

            AddComponent<StuffOwner>(entity);
            StuffGameObjectPrefab stuffGameObjectPrefab = new StuffGameObjectPrefab
            {
                Value = authoring.harvesterPrefab
            };
            StuffCommonData commonData = new StuffCommonData
            {
                name = "Harvester",
                type = StuffType.Harvester,
                side = TeamSideType.Corpo,
                deploymentSpeed = 1.0f,
                storageSpeed = 1.0f,
                price = 0,
                _stuffLocalOffsetView = new Vector3(0.4f, -0.35f, 0.7f)
            };

            AddSharedComponent(entity, commonData);
            AddComponentObject(entity, stuffGameObjectPrefab);
        }
    }
}
