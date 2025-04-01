using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
partial struct CommonCharacterPhysicsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (physicsCollider, characterEntity) in SystemAPI
            .Query<RefRW<PhysicsCollider>>()
            .WithNone<CharacterIsEnable, CharacterDeadColliderTag>()
            .WithAll<CharacterTag>()
            .WithEntityAccess())
        {
            CollisionFilter filter = physicsCollider.ValueRO.Value.Value.GetCollisionFilter();
            filter.CollidesWith &= ~(1u << 6);
            physicsCollider.ValueRO.Value.Value.SetCollisionFilter(filter);
            ecb.AddComponent<CharacterDeadColliderTag>(characterEntity);
        }

        foreach (var (physicsCollider, characterEntity) in SystemAPI
            .Query<RefRW<PhysicsCollider>>()
            .WithAll<CharacterIsEnable, CharacterDeadColliderTag, CharacterTag>()
            .WithEntityAccess())
        {
            CollisionFilter filter = physicsCollider.ValueRO.Value.Value.GetCollisionFilter();
            filter.CollidesWith &= 0xFFFFFFFF;
            physicsCollider.ValueRO.Value.Value.SetCollisionFilter(filter);
            ecb.RemoveComponent<CharacterDeadColliderTag>(characterEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
