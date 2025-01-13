using Unity.Burst;
using Unity.Entities;

public struct ServerDataComponent : IComponentData
{
    public uint playerCount;
    public uint tickRate;
    public float latence;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct ServerDataSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityArchetype archetype = state.EntityManager.CreateArchetype(typeof(ServerDataComponent));
        Entity entity = state.EntityManager.CreateEntity(archetype);
        state.EntityManager.SetComponentData(entity, new ServerDataComponent
        {
            playerCount = 0,
            tickRate = 0,
            latence = 0
        });
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
