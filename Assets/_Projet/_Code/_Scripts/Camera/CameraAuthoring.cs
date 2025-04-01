using Unity.Entities;
using UnityEngine;

class CameraAuthoring : MonoBehaviour
{
    public Vector3 cameraOffset;
}

class CameraAuthoringBaker : Baker<CameraAuthoring>
{
    public override void Bake(CameraAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new CameraComponent
        {
            CurrentTarget = Entity.Null,
            Offset = authoring.cameraOffset,
        });
    }
}
