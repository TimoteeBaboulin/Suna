using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct HitCommand : IRpcCommand
{
    public float3 position;
    public float3 normal;
    public float3 origin;
}