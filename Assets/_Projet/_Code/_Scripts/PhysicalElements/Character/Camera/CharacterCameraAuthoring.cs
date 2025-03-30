using Unity.Entities;
using UnityEngine;


class CharacterCameraAuthoring : MonoBehaviour
{
    public Transform DeltaPosition;
}

class CharacterCameraAuthoringBaker : Baker<CharacterCameraAuthoring>
{
    public override void Bake(CharacterCameraAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<CharacterCameraIsEnable>(entity);
        AddComponent(entity, new CharacterCameraComponent
        {
            CameraFollowEntity = Entity.Null,
            DeltaPosition = authoring.DeltaPosition.position
        });
    }
}
