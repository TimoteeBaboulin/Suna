using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.NetCode;
using Unity.Rendering;

public struct SpawnBarrierComponent : IComponentData
{
    
}

[MaterialProperty("_OpenSince")]
public struct SpawnFenceMaterialOverride : IComponentData
{
    public float Value;
}
