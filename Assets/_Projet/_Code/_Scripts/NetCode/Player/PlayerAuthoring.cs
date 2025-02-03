using Unity.Entities;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PlayerComponent { });
            AddComponent(entity, new PlayerCharacterAttached { Value = Entity.Null });
            AddComponent<WaitForRespawnTag>(entity);
        }
    }
}
