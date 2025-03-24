using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct HarvesterSystemCommon : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<HarvesterComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        EntityQuery query = builder.WithAll<HarvesterComponent>().Build(state.EntityManager);

        NativeArray<Entity> harvesterEntities = query.ToEntityArray(Allocator.Temp);
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach (Entity harvesterEntity in harvesterEntities)
        {
            RefRW<HarvesterComponent> harvesterRW = SystemAPI.GetComponentRW<HarvesterComponent>(harvesterEntity);
            
            if (!SystemAPI.HasComponent<Parent>(harvesterEntity))
            {
                if (harvesterRW.ValueRO.Owner != Entity.Null)
                {
                    Entity characterEntity = SystemAPI.GetComponentRO<ClientCharacterAttached>(harvesterRW.ValueRO.Owner).ValueRO.Value;
                    //ecb.AddComponent(harvesterEntity, new Parent { Value = characterEntity });
                }
            }
            else
            {
                if (harvesterRW.ValueRO.Owner == Entity.Null)
                {
                    Entity parentEntity = SystemAPI.GetComponentRO<Parent>(harvesterEntity).ValueRO.Value;
                    float3 position = SystemAPI.GetComponentRO<LocalTransform>(parentEntity).ValueRO.Position;

                    SystemAPI.GetComponentRW<LocalTransform>(harvesterEntity).ValueRW.Position = position;
                    //ecb.RemoveComponent<Parent>(harvesterEntity);
                }
            }
        }
    }
}