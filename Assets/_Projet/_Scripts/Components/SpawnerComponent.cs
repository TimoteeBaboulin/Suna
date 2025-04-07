using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

public struct SpawnerComponent : IComponentData
{
    public TeamSideType side;
}
