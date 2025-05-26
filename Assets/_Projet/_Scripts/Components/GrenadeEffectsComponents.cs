using Unity.Entities;
using Unity.NetCode;

[GhostComponent]
public struct FlashGrenadeEffect : IComponentData
{
    [GhostField] public float intensity;
}

[GhostComponent]
public struct SmokeGrenadeEffect : IComponentData
{
    [GhostField] public float intensity;
    [GhostField] public bool isSmoke;
}