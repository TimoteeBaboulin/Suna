using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct CommonTestSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
    

        //foreach (var input in SystemAPI.Query<RefRO<PlayerInputData>>())
        //{
        //    if (input.ValueRO.jump.IsSet)
        //    {
        //        Debug.Log("Jump" + state.World);
        //    }
        //}
    }
}
