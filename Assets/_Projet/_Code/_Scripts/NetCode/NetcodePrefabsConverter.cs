using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class NetcodePrefabsConverter : MonoBehaviour
{
    //public GameObject unit = null;
    public GameObject client = null;
    public GameObject character = null;
}

public struct PrefabsData : IComponentData
{
    //public Entity unit;
    public Entity client;
    public Entity character;
    public LocalTransform transformCompData;
}

public class PrefabsBaker : Baker<NetcodePrefabsConverter>
{
    public override void Bake(NetcodePrefabsConverter authoring)
    {
        //Entity unitPrefab = default;
        Entity clientPrefab = default;
        Entity characterPrefab = default;

        LocalTransform transformPrefab = default;

        if (authoring.client != null)
        {
            clientPrefab = GetEntity(authoring.client, TransformUsageFlags.Dynamic);
        }
        if (authoring.character != null)
        {
            characterPrefab = GetEntity(authoring.character, TransformUsageFlags.Dynamic);
            transformPrefab.Position = authoring.character.transform.position;
            transformPrefab.Rotation = authoring.character.transform.rotation;
        }
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PrefabsData
        {
            // unit = unitPrefab,
            client = clientPrefab,
            character = characterPrefab,
            transformCompData = transformPrefab
        });
        AddComponent(entity, new CharacterTag());
    }
}
