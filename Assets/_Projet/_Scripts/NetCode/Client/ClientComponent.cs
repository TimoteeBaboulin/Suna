using Unity.Entities;
using Unity.NetCode;
public struct ClientCharacterAttached : IComponentData
{
    [GhostField] public Entity Value;
}
