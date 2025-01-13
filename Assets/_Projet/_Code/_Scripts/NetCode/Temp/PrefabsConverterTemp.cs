using Unity.Entities;
using UnityEngine;

public class PrefabsConverterTemp : MonoBehaviour
{
    public GameObject prefab = null;
}

public struct PrefabsData : IComponentData
{
    public Entity prefab;
}

public class PrefabsBaker : Baker<PrefabsConverterTemp>
{
    public override void Bake(PrefabsConverterTemp authoring)
    {
        if (authoring.prefab != null)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PrefabsData
            {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}
