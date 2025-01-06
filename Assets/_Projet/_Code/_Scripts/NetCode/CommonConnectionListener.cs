using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
[CreateAfter(typeof(RpcSystem))]
public partial struct SetRpcSystemDynamicAssemblyListSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        SystemAPI.GetSingletonRW<RpcCollection>().ValueRW.DynamicAssemblyList = true;
        state.Enabled = false;
    }
}

public struct GoInGameRequest : IRpcCommand
{
}
