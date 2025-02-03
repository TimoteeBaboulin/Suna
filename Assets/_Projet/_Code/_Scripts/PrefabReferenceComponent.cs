using Unity.Entities.Serialization;
using Unity.Entities;

public struct PrefabReferenceComponent : IComponentData
{
    public Entity prefab;
}