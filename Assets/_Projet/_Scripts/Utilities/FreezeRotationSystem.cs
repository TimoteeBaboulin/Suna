using Unity.Entities;
using Unity.Physics;

public struct FreezeAllRotationTag : IComponentData { }

public partial struct FreezeAllRotationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var mass in SystemAPI.Query<RefRW<PhysicsMass>>()
            .WithAll<FreezeAllRotationTag>())
        {
            mass.ValueRW.InverseInertia = 0f;
        }
    }
}