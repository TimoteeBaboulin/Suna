using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.NetCode;
using Unity.Rendering;

public struct SpawnBarrierComponent : IComponentData
{
    
}

[GhostComponent]
[MaterialProperty("_OpenSince")]
public struct SpawnFenceMaterialOverride : IComponentData
{
    [GhostField] public float Value;
}
