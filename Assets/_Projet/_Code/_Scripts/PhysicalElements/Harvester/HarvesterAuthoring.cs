using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;
using UnityEngine.UI;

public struct HarvesterComponent : IComponentData
{
    public UsableEquipmentType type;
    public FixedString64Bytes entityName;
    public float deploymentSpeed;
    public float storageSpeed;

    public float timer;
    public float interactDistance;
    public Entity activeEffectPrefab;
}

public class HarvesterAuthoring : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Infos")]
    public string entityName;
    public Image UIImage;
    public UsableEquipmentType type;
    public float deploymentSpeed;
    public float storageSpeed;

    [Header("Harveser Data")]
    public float timer;
    public float interactDistance;
    public GameObject activeEffectPrefab;

    public class Baker : Baker<HarvesterAuthoring>
    {
        public override void Bake(HarvesterAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PrefabReferenceComponent
            {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
            });

            AddComponent(entity, new HarvesterComponent
            {
                entityName = authoring.entityName,
                type = authoring.type,
                deploymentSpeed = authoring.deploymentSpeed,
                storageSpeed = authoring.storageSpeed,
                timer = authoring.timer,
                interactDistance = authoring.interactDistance,
                //activeEffectPrefab = authoring.activeEffectPrefab,
    });
        }
    }
}
