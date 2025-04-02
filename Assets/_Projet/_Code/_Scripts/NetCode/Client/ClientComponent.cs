using Unity.Entities;
using Unity.NetCode;

public struct ClientComponent : IComponentData
{
    
}

public struct ClientCharacterAttached : IComponentData
{
    [GhostField] public Entity Value;
}
