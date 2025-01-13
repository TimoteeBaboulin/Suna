using Unity.Entities;
using UnityEngine;

public class PrefabsConverterTemp : MonoBehaviour
{
    public GameObject unit = null;
    public GameObject player = null;
}

public struct PrefabsData : IComponentData
{
    public Entity unit;
    public Entity player;
}

public class PrefabsBaker : Baker<PrefabsConverterTemp>
{
    public override void Bake(PrefabsConverterTemp authoring)
    {
        if (authoring.unit != null)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PrefabsData
            {
                unit = GetEntity(authoring.unit, TransformUsageFlags.Dynamic)
            });
        }
    }
}
