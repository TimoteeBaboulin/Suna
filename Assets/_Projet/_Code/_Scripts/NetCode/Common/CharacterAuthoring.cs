using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CharacterAuthoring : MonoBehaviour
{
    public class Baker : Baker<CharacterAuthoring>
    {
        public override void Bake(CharacterAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ShootInputComponent>(entity);
            AddComponent<LookInputComponent>(entity);
            AddComponent(entity, new CameraRotationComponent { Value = quaternion.identity });
        }
    }
}
