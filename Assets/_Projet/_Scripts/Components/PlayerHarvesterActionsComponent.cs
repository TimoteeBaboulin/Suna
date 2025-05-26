using Unity.Entities;
using Unity.NetCode;

[GhostComponent]
public partial struct PlayerHarvesterActions : IComponentData
{
    [GhostField] public bool IsDefusing;
    [GhostField] public bool IsPlanting;
}