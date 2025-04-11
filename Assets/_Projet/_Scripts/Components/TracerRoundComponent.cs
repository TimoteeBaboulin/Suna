using Unity.Entities;
using Unity.Mathematics;

public struct TracerRoundComponent : IComponentData
{
    public float3 start;
    public float3 end;

    public float speed;
}
