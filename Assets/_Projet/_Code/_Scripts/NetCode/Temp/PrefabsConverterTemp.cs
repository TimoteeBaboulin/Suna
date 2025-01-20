using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class PrefabsConverterTemp : MonoBehaviour
{
    //public GameObject unit = null;
    public GameObject player = null;
}

public struct PrefabsData : IComponentData
{
    //public Entity unit;
    public Entity player;
    public LocalTransform transformCompData;
}

public class PrefabsBaker : Baker<PrefabsConverterTemp>
{
    public override void Bake(PrefabsConverterTemp authoring)
    {
        //Entity unitPrefab = default;
        Entity playerPrefab = default;
        LocalTransform transformPrefab = default;
        if (authoring.player != null)
        {
            playerPrefab = GetEntity(authoring.player, TransformUsageFlags.Dynamic);
        }
        if (authoring.player != null)
        {
            transformPrefab.Position = authoring.player.transform.position;
            transformPrefab.Rotation = authoring.player.transform.rotation;
        }
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PrefabsData
        {
           // unit = unitPrefab,
            player = playerPrefab,
            transformCompData = transformPrefab
        });
        AddComponent(entity, new CharacterTag());
    }
}
