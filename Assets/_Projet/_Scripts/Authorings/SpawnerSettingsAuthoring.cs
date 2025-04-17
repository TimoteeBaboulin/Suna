using Unity.Entities;
using UnityEngine;

class SpawnerSettingsAuthoring : MonoBehaviour
{
    public bool AutoRespawnIsEnable;
}

class SpawnerSettingsAuthoringBaker : Baker<SpawnerSettingsAuthoring>
{
    public override void Bake(SpawnerSettingsAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent<SpawnerSettingsTag>(entity);
        AddComponent<AutoRespawnIsEnable>(entity);

        if (authoring.AutoRespawnIsEnable)
        {
            SetComponentEnabled<AutoRespawnIsEnable>(entity, true);
        }
        else
        {
            SetComponentEnabled<AutoRespawnIsEnable>(entity, false);
        }
    }
}
