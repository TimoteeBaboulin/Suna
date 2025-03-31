using Unity.Entities;
using UnityEngine;

class CameraFollowAuthoring : MonoBehaviour
{
    
}

class CameraFollowAuthoringBaker : Baker<CameraFollowAuthoring>
{
    public override void Bake(CameraFollowAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<CameraFollowIsEnable>(entity);
        SetComponentEnabled<CameraFollowIsEnable>(entity, false);
    }
}
