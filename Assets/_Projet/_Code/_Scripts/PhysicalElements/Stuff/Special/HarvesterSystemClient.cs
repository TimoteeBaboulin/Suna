using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

partial struct HarvesterSystemClient : ISystem
{
    public struct HarvesterStartPlant : IRpcCommand
    {
        NetworkTime startTime;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRW<HarvesterComponent> harvester, Entity harvesterEntity) 
            in SystemAPI.Query<RefRW<HarvesterComponent>>().WithEntityAccess())
        {
            if (!state.EntityManager.Exists(harvester.ValueRO.owner))
                continue;

            Entity owner = harvester.ValueRO.owner;
            if (!state.EntityManager.HasComponent<CharacterInput>(owner))
                continue;

            CharacterInput input = state.EntityManager.GetComponentData<CharacterInput>(owner);

            if (input.shoot.IsSet)
            {
                UnityEngine.Debug.Log("Shoot is set");
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
