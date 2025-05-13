using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

public class NetcodePrefabsConverter : MonoBehaviour
{
    //public GameObject unit = null;
    [Header("Client Prefabs")]
    public GameObject Client = null;

    [Header("Character Prefabs")]
    public GameObject Character = null;

    [Header("Corpo Collider Prefabs")]
    public GameObject CorpoHeadCollider = null;
    public GameObject CorpoArmCollider0 = null;
    public GameObject CorpoArmCollider1 = null;
    public GameObject CorpoArmCollider2 = null;
    public GameObject CorpoThoraxCollider = null;
    public GameObject CorpoStomachCollider0 = null;
    public GameObject CorpoStomachCollider1 = null;
    public GameObject CorpoLegCollider0 = null;
    public GameObject CorpoLegCollider1 = null;
    public GameObject CorpoLegCollider2 = null;

    [Header("Natif Collider Prefabs")]
    public GameObject NatifHeadCollider = null;
    public GameObject NatifArmCollider0 = null;
    public GameObject NatifArmCollider1 = null;
    public GameObject NatifArmCollider2 = null;
    public GameObject NatifThoraxCollider = null;
    public GameObject NatifStomachCollider0 = null;
    public GameObject NatifStomachCollider1 = null;
    public GameObject NatifLegCollider0 = null;
    public GameObject NatifLegCollider1 = null;
    public GameObject NatifLegCollider2 = null;

    [Header("Visual Elements Prefabs")]
    public GameObject hitPrefab = null;
    public GameObject tracerRoundVfxPrefab = null;

    public GameObject heGrenadeExplosion = null;
    public GameObject flashbangExplosion = null;
}

public struct ClientPrefabData : IComponentData
{
    //public Entity unit;
    public Entity Client;

    public Entity Character;

    public Entity CorpoHeadCollider;
    public Entity CorpoArmCollider0;
    public Entity CorpoArmCollider1;
    public Entity CorpoArmCollider2;
    public Entity CorpoThoraxCollider;
    public Entity CorpoStomachCollider0;
    public Entity CorpoStomachCollider1;
    public Entity CorpoLegCollider0;
    public Entity CorpoLegCollider1;
    public Entity CorpoLegCollider2;

    public Entity NatifHeadCollider;
    public Entity NatifArmCollider0;
    public Entity NatifArmCollider1;
    public Entity NatifArmCollider2;
    public Entity NatifThoraxCollider;
    public Entity NatifStomachCollider0;
    public Entity NatifStomachCollider1;
    public Entity NatifLegCollider0;
    public Entity NatifLegCollider1;
    public Entity NatifLegCollider2;

    public LocalTransform TransformCompData;
}

public struct VisualEffetPrefabData : IComponentData
{
    public Entity hitVisualEffect;
    public Entity tracerRoundVisualEffect;

    public Entity heGrenadeExplosion;
    public Entity flashbangExplosion;
}

public struct VFXDurationData : IComponentData
{
    public float hitVFXDuration;
    public float tracerVFXDuration;

    public float heGrenadeExplosionDuration;
    public float flashbangExplosionDuration;
}

public class PrefabsBaker : Baker<NetcodePrefabsConverter>
{
    public override void Bake(NetcodePrefabsConverter authoring)
    {
        //Entity unitPrefab = default;
        Entity clientPrefab = default;

        Entity characterPrefab = default;

        Entity corpoHeadCollider = default;
        Entity corpoArmCollider0 = default;
        Entity corpoArmCollider1 = default;
        Entity corpoArmCollider2 = default;
        Entity corpoThoraxCollider = default;
        Entity corpoStomachCollider0 = default;
        Entity corpoStomachCollider1 = default;
        Entity corpoLegCollider0 = default;
        Entity corpoLegCollider1 = default;
        Entity corpoLegCollider2 = default;

        Entity natifHeadCollider = default;
        Entity natifArmCollider0 = default;
        Entity natifArmCollider1 = default;
        Entity natifArmCollider2 = default;
        Entity natifThoraxCollider = default;
        Entity natifStomachCollider0 = default;
        Entity natifStomachCollider1 = default;
        Entity natifLegCollider0 = default;
        Entity natifLegCollider1 = default;
        Entity natifLegCollider2 = default;

        LocalTransform transformPrefab = default;

        Entity hitEffect = default;
        Entity tracerEffect = default;
        Entity heGrenadeExplosion = default;
        Entity flashbangExplosion = default;
        // VFX durations
        float hitDuration = 1.0f;
        float tracerDuration = 1.0f;
        float heGrenadeExplosionDuration = 1.0f;
        float flashbangExplosionDuration = 1.0f;

        hitEffect = GetVFXEntityWithDuration(authoring.hitPrefab, TransformUsageFlags.Dynamic, out hitDuration);
        tracerEffect = GetVFXEntityWithDuration(authoring.tracerRoundVfxPrefab, TransformUsageFlags.Dynamic, out tracerDuration);
        heGrenadeExplosion = GetVFXEntityWithDuration(authoring.heGrenadeExplosion, TransformUsageFlags.Dynamic, out heGrenadeExplosionDuration);
        flashbangExplosion = GetVFXEntityWithDuration(authoring.flashbangExplosion, TransformUsageFlags.Dynamic, out flashbangExplosionDuration);

        //Coucou ici Aurelien


        if (authoring.hitPrefab != null)
        {
            hitEffect = GetEntity(authoring.hitPrefab, TransformUsageFlags.Dynamic);
        }
        if (authoring.tracerRoundVfxPrefab != null)
        {
            tracerEffect = GetEntity(authoring.tracerRoundVfxPrefab, TransformUsageFlags.Dynamic);
        }
        if (authoring.heGrenadeExplosion != null)
        {
            heGrenadeExplosion = GetEntity(authoring.heGrenadeExplosion, TransformUsageFlags.Dynamic);
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

        if (authoring.CorpoHeadCollider != null)
        {
            corpoHeadCollider = GetEntity(authoring.CorpoHeadCollider, TransformUsageFlags.Dynamic);
        }
        if (authoring.CorpoArmCollider0 != null)
        {
            corpoArmCollider0 = GetEntity(authoring.CorpoArmCollider0, TransformUsageFlags.Dynamic);
        }
        if (authoring.CorpoArmCollider1 != null)
        {
            corpoArmCollider1 = GetEntity(authoring.CorpoArmCollider1, TransformUsageFlags.Dynamic);
        }
        if (authoring.CorpoArmCollider2 != null)
        {
            corpoArmCollider2 = GetEntity(authoring.CorpoArmCollider2, TransformUsageFlags.Dynamic);
        }
        if (authoring.CorpoThoraxCollider != null)
        {
            corpoThoraxCollider = GetEntity(authoring.CorpoThoraxCollider, TransformUsageFlags.Dynamic);
        }
        if (authoring.CorpoStomachCollider0 != null)
        {
            corpoStomachCollider0 = GetEntity(authoring.CorpoStomachCollider0, TransformUsageFlags.Dynamic);
        }
        if (authoring.CorpoStomachCollider1 != null)
        {
            corpoStomachCollider1 = GetEntity(authoring.CorpoStomachCollider1, TransformUsageFlags.Dynamic);
        }
        if (authoring.CorpoLegCollider0 != null)
        {
            corpoLegCollider0 = GetEntity(authoring.CorpoLegCollider0, TransformUsageFlags.Dynamic);
        }
        if (authoring.CorpoLegCollider1 != null)
        {
            corpoLegCollider1 = GetEntity(authoring.CorpoLegCollider1, TransformUsageFlags.Dynamic);
        }
        if (authoring.CorpoLegCollider2 != null)
        {
            corpoLegCollider2 = GetEntity(authoring.CorpoLegCollider2, TransformUsageFlags.Dynamic);
        }

        if (authoring.NatifHeadCollider != null)
        {
            natifHeadCollider = GetEntity(authoring.NatifHeadCollider, TransformUsageFlags.Dynamic);
        }
        if (authoring.NatifArmCollider0 != null)
        {
            natifArmCollider0 = GetEntity(authoring.NatifArmCollider0, TransformUsageFlags.Dynamic);
        }
        if (authoring.NatifArmCollider1 != null)
        {
            natifArmCollider1 = GetEntity(authoring.NatifArmCollider1, TransformUsageFlags.Dynamic);
        }
        if (authoring.NatifArmCollider2 != null)
        {
            natifArmCollider2 = GetEntity(authoring.NatifArmCollider2, TransformUsageFlags.Dynamic);
        }
        if (authoring.NatifThoraxCollider != null)
        {
            natifThoraxCollider = GetEntity(authoring.NatifThoraxCollider, TransformUsageFlags.Dynamic);
        }
        if (authoring.NatifStomachCollider0 != null)
        {
            natifStomachCollider0 = GetEntity(authoring.NatifStomachCollider0, TransformUsageFlags.Dynamic);
        }
        if (authoring.NatifStomachCollider1 != null)
        {
            natifStomachCollider1 = GetEntity(authoring.NatifStomachCollider1, TransformUsageFlags.Dynamic);
        }
        if (authoring.NatifLegCollider0 != null)
        {
            natifLegCollider0 = GetEntity(authoring.NatifLegCollider0, TransformUsageFlags.Dynamic);
        }
        if (authoring.NatifLegCollider1 != null)
        {
            natifLegCollider1 = GetEntity(authoring.NatifLegCollider1, TransformUsageFlags.Dynamic);
        }
        if (authoring.NatifLegCollider2 != null)
        {
            natifLegCollider2 = GetEntity(authoring.NatifLegCollider2, TransformUsageFlags.Dynamic);
        }

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ClientPrefabData
        {
            // unit = unitPrefab,
            Client = clientPrefab,

            Character = characterPrefab,
            
            CorpoHeadCollider = corpoHeadCollider,
            CorpoArmCollider0 = corpoArmCollider0,
            CorpoArmCollider1 = corpoArmCollider1,
            CorpoArmCollider2 = corpoArmCollider2,
            CorpoThoraxCollider = corpoThoraxCollider,
            CorpoStomachCollider0 = corpoStomachCollider0,
            CorpoStomachCollider1 = corpoStomachCollider1,
            CorpoLegCollider0 = corpoLegCollider0,
            CorpoLegCollider1 = corpoLegCollider1,
            CorpoLegCollider2 = corpoLegCollider2,

            NatifHeadCollider = natifHeadCollider,
            NatifArmCollider0 = natifArmCollider0,
            NatifArmCollider1 = natifArmCollider1,
            NatifArmCollider2 = natifArmCollider2,
            NatifThoraxCollider = natifThoraxCollider,
            NatifStomachCollider0 = natifStomachCollider0,
            NatifStomachCollider1 = natifStomachCollider1,
            NatifLegCollider0 = natifLegCollider0,
            NatifLegCollider1 = natifLegCollider1,
            NatifLegCollider2 = natifLegCollider2,

            TransformCompData = transformPrefab,
        });
        AddComponent(entity, new CharacterTag());

        Entity VisualEffectentity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(VisualEffectentity, new VisualEffetPrefabData
        {
            hitVisualEffect = hitEffect,
            tracerRoundVisualEffect = tracerEffect,
            heGrenadeExplosion = heGrenadeExplosion,
            flashbangExplosion = flashbangExplosion
        });

        AddComponent(entity, new VFXDurationData
        {
            hitVFXDuration = hitDuration,
            tracerVFXDuration = tracerDuration,
            heGrenadeExplosionDuration = heGrenadeExplosionDuration,
            flashbangExplosionDuration = flashbangExplosionDuration
        });
    }

    private Entity GetVFXEntityWithDuration(GameObject prefab, TransformUsageFlags usageFlags, out float duration)
    {
        duration = 1.0f;

        if (prefab == null)
            return Entity.Null;

        var vfx = prefab.GetComponent<VisualEffect>();
        if (vfx != null && vfx.HasFloat("duration"))
        {
            duration = vfx.GetFloat("duration");
        }

        return GetEntity(prefab, usageFlags);
    }
}
