using Unity.Entities;
using UnityEngine;

public class ClientAuthoring : MonoBehaviour
{
    public class Baker : Baker<ClientAuthoring>
    {
        public override void Bake(ClientAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ClientComponent { });
            AddComponent(entity, new ClientCharacterAttached { Value = Entity.Null });
            AddComponent<WaitForRespawnTag>(entity);
        }
    }
}
