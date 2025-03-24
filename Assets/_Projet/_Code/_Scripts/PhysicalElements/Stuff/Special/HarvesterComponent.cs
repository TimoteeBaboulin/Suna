using Unity.Entities;
using Unity.NetCode;

[GhostComponent]
partial struct HarvesterComponent : IComponentData
{
    [GhostField] public Entity Owner;
}
