using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class NetcodePrefabsConverter : MonoBehaviour
{
    //public GameObject unit = null;
    public GameObject Client = null;
    public GameObject Character = null;

    [Header("Character Collider Prefabs")]
    public GameObject CharacterHeadCollider = null;
    public GameObject CharacterArmCollider0 = null;
    public GameObject CharacterArmCollider1 = null;
    public GameObject CharacterArmCollider2 = null;
    public GameObject CharacterThoraxCollider = null;
    public GameObject CharacterStomachCollider0 = null;
    public GameObject CharacterStomachCollider1 = null;
    public GameObject CharacterLegCollider0 = null;
    public GameObject CharacterLegCollider1 = null;
    public GameObject CharacterLegCollider2 = null;

    [Header("Visual Elements Prefabs")]
    public GameObject hitPrefab = null;
}

public struct ClientPrefabData : IComponentData
{
    //public Entity unit;
    public Entity Client;
    public Entity Character;
    public LocalTransform TransformCompData;

    public Entity CharacterHeadCollider;
    public Entity CharacterArmCollider0;
    public Entity CharacterArmCollider1;
    public Entity CharacterArmCollider2;
    public Entity CharacterThoraxCollider;
    public Entity CharacterStomachCollider0;
    public Entity CharacterStomachCollider1;
    public Entity CharacterLegCollider0;
    public Entity CharacterLegCollider1;
    public Entity CharacterLegCollider2;
}

public struct VisualEffetPrefabData : IComponentData
{
    public Entity hitVisualEffect;
}

public class PrefabsBaker : Baker<NetcodePrefabsConverter>
{
    public override void Bake(NetcodePrefabsConverter authoring)
    {
        //Entity unitPrefab = default;
        Entity clientPrefab = default;
        Entity characterPrefab = default;
        Entity characterHeadCollider = default;
        Entity characterArmCollider0 = default;
        Entity characterArmCollider1 = default;
        Entity characterArmCollider2 = default;
        Entity characterThoraxCollider = default;
        Entity characterStomachCollider0 = default;
        Entity characterStomachCollider1 = default;
        Entity characterLegCollider0 = default;
        Entity characterLegCollider1 = default;
        Entity characterLegCollider2 = default;

        LocalTransform transformPrefab = default;
        //Coucou ici Aurelien
        Entity hitEffect = default;
        if (authoring.hitPrefab != null)
        {
            hitEffect = GetEntity(authoring.hitPrefab, TransformUsageFlags.Dynamic);
        }

        if (authoring.Client != null)
        {
            clientPrefab = GetEntity(authoring.Client, TransformUsageFlags.Dynamic);
        }
        if (authoring.Character != null)
        {
            characterPrefab = GetEntity(authoring.Character, TransformUsageFlags.Dynamic);
            transformPrefab.Position = authoring.Character.transform.position;
            transformPrefab.Rotation = authoring.Character.transform.rotation;
        }
        if (authoring.CharacterHeadCollider != null)
        {
            characterHeadCollider = GetEntity(authoring.CharacterHeadCollider, TransformUsageFlags.Dynamic);
        }
        if (authoring.CharacterArmCollider0 != null)
        {
            characterArmCollider0 = GetEntity(authoring.CharacterArmCollider0, TransformUsageFlags.Dynamic);
        }
        if (authoring.CharacterArmCollider1 != null)
        {
            characterArmCollider1 = GetEntity(authoring.CharacterArmCollider1, TransformUsageFlags.Dynamic);
        }
        if (authoring.CharacterArmCollider2 != null)
        {
            characterArmCollider2 = GetEntity(authoring.CharacterArmCollider2, TransformUsageFlags.Dynamic);
        }
        if (authoring.CharacterThoraxCollider != null)
        {
            characterThoraxCollider = GetEntity(authoring.CharacterThoraxCollider, TransformUsageFlags.Dynamic);
        }
        if (authoring.CharacterStomachCollider0 != null)
        {
            characterStomachCollider0 = GetEntity(authoring.CharacterStomachCollider0, TransformUsageFlags.Dynamic);
        }
        if (authoring.CharacterStomachCollider1 != null)
        {
            characterStomachCollider1 = GetEntity(authoring.CharacterStomachCollider1, TransformUsageFlags.Dynamic);
        }
        if (authoring.CharacterLegCollider0 != null)
        {
            characterLegCollider0 = GetEntity(authoring.CharacterLegCollider0, TransformUsageFlags.Dynamic);
        }
        if (authoring.CharacterLegCollider1 != null)
        {
            characterLegCollider1 = GetEntity(authoring.CharacterLegCollider1, TransformUsageFlags.Dynamic);
        }
        if (authoring.CharacterLegCollider2 != null)
        {
            characterLegCollider2 = GetEntity(authoring.CharacterLegCollider2, TransformUsageFlags.Dynamic);
        }

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ClientPrefabData
        {
            // unit = unitPrefab,
            Client = clientPrefab,
            Character = characterPrefab,
            TransformCompData = transformPrefab,
            CharacterHeadCollider = characterHeadCollider,
            CharacterArmCollider0 = characterArmCollider0,
            CharacterArmCollider1 = characterArmCollider1,
            CharacterArmCollider2 = characterArmCollider2,
            CharacterThoraxCollider = characterThoraxCollider,
            CharacterStomachCollider0 = characterStomachCollider0,
            CharacterStomachCollider1 = characterStomachCollider1,
            CharacterLegCollider0 = characterLegCollider0,
            CharacterLegCollider1 = characterLegCollider1,
            CharacterLegCollider2 = characterLegCollider2,
        });
        AddComponent(entity, new CharacterTag());

        Entity VisualEffectentity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(VisualEffectentity, new VisualEffetPrefabData
        {
            hitVisualEffect = hitEffect
        });
    }
}
