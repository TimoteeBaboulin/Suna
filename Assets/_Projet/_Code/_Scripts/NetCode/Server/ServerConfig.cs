using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ClientServerTickRateSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityArchetype entityArchetype = state.EntityManager.CreateArchetype(typeof(ClientServerTickRate));
        Entity entity = state.EntityManager.CreateEntity(entityArchetype);

        state.EntityManager.SetComponentData(entity, new ClientServerTickRate
        {
            NetworkTickRate = 60,
            SimulationTickRate = 60
        });
    }
}
