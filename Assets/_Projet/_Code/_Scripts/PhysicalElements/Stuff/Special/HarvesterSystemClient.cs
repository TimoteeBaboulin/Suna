using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

public struct HarvesterStartPlant : IRpcCommand
{

}

partial struct HarvesterSystemClient : ISystem
{
    

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //TODO: Tell the server to start/cancel the plant
        foreach ((RefRW<HarvesterComponent> harvester, Entity harvesterEntity) 
            in SystemAPI.Query<RefRW<HarvesterComponent>>().WithEntityAccess())
        {
            //if (!state.EntityManager.Exists(harvester.ValueRO.owner))
            //    continue;

            //Entity owner = harvester.ValueRO.;
            //if (!state.EntityManager.HasComponent<CharacterInput>(owner))
            //    continue;

            //CharacterInput input = state.EntityManager.GetComponentData<CharacterInput>(owner);

            //if (input.shoot.IsSet)
            //{
            //    UnityEngine.Debug.Log("Shoot is set");
            //}
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
