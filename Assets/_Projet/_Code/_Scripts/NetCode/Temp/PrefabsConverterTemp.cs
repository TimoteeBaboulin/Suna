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
        Entity unitPrefab = default;
        Entity playerPrefab = default;
        if (authoring.unit != null)
        {
            unitPrefab = GetEntity(authoring.unit, TransformUsageFlags.Dynamic);
        }
        if (authoring.player != null)
        {
            playerPrefab = GetEntity(authoring.player, TransformUsageFlags.Dynamic);
        }
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PrefabsData
        {
            unit = unitPrefab,
            player = playerPrefab
        });
    }
}
