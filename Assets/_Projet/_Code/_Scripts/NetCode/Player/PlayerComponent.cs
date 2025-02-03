using Unity.Entities;
using Unity.NetCode;

public struct PlayerComponent : IComponentData
{
    
}

public struct PlayerCharacterAttached : IComponentData
{
    [GhostField] public Entity Value;
}
