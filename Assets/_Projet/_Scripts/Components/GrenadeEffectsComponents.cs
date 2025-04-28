using Unity.Entities;
using Unity.NetCode;

[GhostComponent]
public struct FlashGrenadeEffect : IComponentData
{
    [GhostField] public float intensity;
}