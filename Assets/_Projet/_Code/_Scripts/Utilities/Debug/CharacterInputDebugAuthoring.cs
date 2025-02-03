using Unity.Entities;
using UnityEngine;

public class CharacterInputDebugAuthoring : MonoBehaviour
{
    public class Baker : Baker<CharacterInputDebugAuthoring>
    {
        public override void Bake(CharacterInputDebugAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<CharacterDebugInputComponent>(entity);
        }
    }
}
