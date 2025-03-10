using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(5)]
public struct SpawnPointBufferComponent : IBufferElementData
{
    public static implicit operator float3 (SpawnPointBufferComponent instance) { return instance.Value; }
    public static implicit operator SpawnPointBufferComponent(float3 val) { return new SpawnPointBufferComponent { Value = val }; }

    public float3 Value;
}
