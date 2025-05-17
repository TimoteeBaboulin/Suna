using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public enum StuffSlot
{
    MainWeapon,
    SecondaryWeapon,
    Melee,
    Harvester,
    HEGrenade,
    Flashbang,

    nbSlots
}

public enum StuffType
{
    RangedWeapon,
    MeleeWeapon,
    Grenade,
    Harvester,
}

public enum StuffState
{
    InShop,
    Instantiate,
    Equip,
    Unequip,
    Throw,
    Drop,
    Plant
}

[GhostComponent]
public struct StuffDatabaseAccess : IComponentData
{
    [GhostField] public int ID;
    [GhostField] public bool IsConnectedToDatabase;
    [GhostField] public FixedString128Bytes NameInDatabase;

    public ref StuffCommonData GetData(ref GameResourcesDatabase database)
    {
        return ref database.StuffDatabaseRef.Value.StuffCommonData[ID];
    }
}

[GhostComponent]
public struct StuffEntityInHandRef : IComponentData
{
    [GhostField] public Entity Value;
}

[GhostComponent]
public struct StuffDynamicData : IComponentData
{
    [GhostField] public StuffState state;

    [GhostField] public Entity owner;
    [GhostField] public Entity dropedEntityPrefab;
    [GhostField] public Entity dropedEntityRef;
    [GhostField] public Entity grenadeThrownPrefab; //Only useful for grenades but I didn't have time to refactor this, sorry
}

[GhostEnabledBit]
[GhostComponent]
public struct IsStuffInHand : IComponentData, IEnableableComponent
{
}

[GhostEnabledBit]
[GhostComponent]
public struct StuffProcessPending : IComponentData, IEnableableComponent
{
    [GhostField] public Entity Owner;
    [GhostField] public float3 Position;
}

public struct StuffCommonData
{
    public BlobString Name;
    public StuffSlot slot;
    public StuffType type;
    public TeamSideType side;
    public float deploymentSpeed;
    public float storageSpeed;
    public int price;
    public float3 _stuffLocalOffsetView;
    public float3 _stuffLocalOffsetView_Baked;
    public uint killGain;

    public bool canADS;
    public float ADSFOV;

    public int dataID;

    public float3 GetStuffLocalOffsetView(TeamSideType side)
    {
        switch (side)
        {
            case TeamSideType.Corpo:
                return _stuffLocalOffsetView_Baked;
            case TeamSideType.Natif:
                return _stuffLocalOffsetView;
            case TeamSideType.Neutre:
                break;
            default:
                break;
        }
        return default;
    }
}

public class StuffGameObjectRef : ICleanupComponentData
{
    public GameObject View;
    public GameObject View_Baked;

    public void Instantiate(GameResourcesViewPrefabs grViewPrefabs, RefRO<StuffDatabaseAccess> stuffDataRef)
    {
        StuffGameObjectRef goRef = new StuffGameObjectRef();

        if (grViewPrefabs.List[stuffDataRef.ValueRO.ID] != null)
        {
            View = GameObject.Instantiate(grViewPrefabs.List[stuffDataRef.ValueRO.ID]);
            View.name = grViewPrefabs.List[stuffDataRef.ValueRO.ID].name;
        }

        if (grViewPrefabs.List_Baked[stuffDataRef.ValueRO.ID] != null)
        {
            View_Baked = GameObject.Instantiate(grViewPrefabs.List_Baked[stuffDataRef.ValueRO.ID]);
            View_Baked.name = grViewPrefabs.List_Baked[stuffDataRef.ValueRO.ID].name + "_Baked";
        }
    }

    public GameObject GetGameObjectSide(TeamSideType side)
    {
        switch (side)
        {
            case TeamSideType.Corpo:
                return View_Baked;
            case TeamSideType.Natif:
                return View;
            case TeamSideType.Neutre:
                return null;
            default:
                return null;
        }
    }

    public Transform GetTransformSide(TeamSideType side)
    {
        switch (side)
        {
            case TeamSideType.Corpo:
                return View_Baked != null ? View_Baked.transform : null;
            case TeamSideType.Natif:
                return View != null ? View.transform : null;
            case TeamSideType.Neutre:
                return null;
            default:
                return null;
        }
    }

    public void SwitchSetActiveSide(TeamSideType side, bool isActive)
    {
        switch (side)
        {
            case TeamSideType.Corpo:

                if (View_Baked != null)
                {
                    View_Baked.SetActive(isActive);
                }
                if (View != null) View.SetActive(false);
                break;
            case TeamSideType.Natif:
                if (View_Baked != null) View_Baked.SetActive(false);
                if (View != null) View.SetActive(isActive);
                break;
            case TeamSideType.Neutre:
                break;
            default:
                break;

        }
    }
    public void SetActive(bool isActive)
    {
        if (View != null) View.SetActive(isActive);
        if (View_Baked != null) View_Baked.SetActive(isActive);
    }
    public void SetActiveOne(bool isActive)
    {
        if (View_Baked != null)
        {
            View_Baked.SetActive(isActive);
            if (View != null) View.SetActive(false);
        }
        else if (View != null)
        {
            View.SetActive(isActive);
            if (View_Baked != null) View_Baked.SetActive(false);
        }
    }

    public void SetParent(Transform parent)
    {
        if (View_Baked != null)
        {
            //Debug.Log("<color=red>SetParent : </color>" + View_Baked_.name + " <color=red>to</color> " + parent);
            View_Baked.transform.SetParent(parent);
        }
        if (View != null)
        {
            //Debug.Log("<color=red>SetParent : </color>" + View.name + " <color=red>to</color> " + parent);
            View.transform.SetParent(parent);
        }
    }

    public void SetLayer(Entity owner, EntityManager entityManager)
    {
        if (owner == Entity.Null)
        {
            if (View_Baked != null)
            {
                SetLayerAllChild(View_Baked, 0);
            }

            if (View != null)
            {
                SetLayerAllChild(View, 0);
            }

            return;
        }

        if (!entityManager.HasComponent<GhostOwner>(owner)) return;

        int networkId = entityManager.GetComponentData<GhostOwner>(owner).NetworkId;
        TeamSideType teamSide = PlayerHelpers.GetPlayerInTeam(networkId);

        if (teamSide == TeamSideType.Neutre)
        {
            if (View_Baked != null)
            {
                SetLayerAllChild(View_Baked, 0);
            }

            if (View != null)
            {
                SetLayerAllChild(View, 0);
            }

            return;
        }

        if (entityManager.HasComponent<GhostOwnerIsLocal>(owner)
            && entityManager.IsComponentEnabled<GhostOwnerIsLocal>(owner))
        {
            if (View_Baked != null)
            {
                SetLayerAllChild(View_Baked, 15);
            }

            if (View != null)
            {
                SetLayerAllChild(View, 15);
            }

            return;
        }

        if (entityManager.HasComponent<CameraIsAtached>(owner))
        {
            if (View_Baked != null)
            {
                SetLayerAllChild(View_Baked, 15);
            }

            if (View != null)
            {
                SetLayerAllChild(View, 15);
            }

            return;
        }

        EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CharacterComponent>(), ComponentType.ReadOnly<GhostOwnerIsLocal>());
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);

        if (entities.Length == 0) return;

        networkId = entityManager.GetComponentData<GhostOwner>(entities[0]).NetworkId;
        TeamSideType clientLocalTeamSide = PlayerHelpers.GetPlayerInTeam(networkId);

        if (clientLocalTeamSide == teamSide)
        {
            if (View_Baked != null)
            {
                SetLayerAllChild(View_Baked, 13);
            }

            if (View != null)
            {
                SetLayerAllChild(View, 13);
            }

            return;
        }
        else
        {
            if (View_Baked != null)
            {
                SetLayerAllChild(View_Baked, 14);
            }

            if (View != null)
            {
                SetLayerAllChild(View, 14);
            }

            return;
        }
    }

    public void SetTransform(Transform transform)
    {
        if (View_Baked != null)
        {
            View_Baked.transform.position = transform.position;
            View_Baked.transform.rotation = transform.rotation;
        }

        if (View != null)
        {
            View.transform.position = transform.position;
            View.transform.rotation = transform.rotation;
        }
    }

    public void SetTransform(float3 position, quaternion rotation)
    {
        if (View_Baked != null)
        {
            View_Baked.transform.position = position;
            View_Baked.transform.rotation = rotation;
        }

        if (View != null)
        {
            View.transform.position = position;
            View.transform.rotation = rotation;
        }
    }

    public void SetLocalTransform(float3 position, quaternion rotation)
    {
        if (View_Baked != null)
        {
            View_Baked.transform.localPosition = position;
            View_Baked.transform.rotation = rotation;
        }

        if (View != null)
        {
            View.transform.localPosition = position;
            View.transform.rotation = rotation;
        }
    }

    public void SetTransform(LocalTransform transform)
    {
        if (View_Baked != null)
        {
            View_Baked.transform.position = transform.Position;
            View_Baked.transform.rotation = transform.Rotation;
        }

        if (View != null)
        {
            View.transform.position = transform.Position;
            View.transform.rotation = transform.Rotation;
        }
    }

    public Transform GetOneTransform()
    {
        if (View_Baked != null) return View_Baked.transform;
        if (View != null) return View.transform;
        return null;
    }

    public void SetLocalScale(float scale)
    {
        if (View_Baked != null) View_Baked.transform.localScale = Vector3.one * scale;
        if (View != null) View.transform.localScale = Vector3.one * scale;
    }

    public void Destroy()
    {
        if (View != null)
        {
            GameObject.Destroy(View);
        }
        if (View_Baked != null)
        {
            GameObject.Destroy(View_Baked);
        }
    }

    private void SetLayerAllChild(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        Transform[] allChildren = obj.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            child.gameObject.layer = newLayer;
        }
    }
}



