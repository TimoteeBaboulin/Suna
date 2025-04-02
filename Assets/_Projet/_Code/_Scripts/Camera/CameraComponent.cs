using Unity.Entities;
using Unity.Mathematics;

public struct CameraComponent : IComponentData
{
    public Entity CurrentTarget;
    public float3 Offset;
}
