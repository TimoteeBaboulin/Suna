using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerConnectionListener : ISystem
{
    private ComponentLookup<NetworkId> networkIdFromEntity;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<GoInGameRequest>()
            .WithAll<ReceiveRpcCommandRequest>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
        networkIdFromEntity = state.GetComponentLookup<NetworkId>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        FixedString128Bytes worldName = state.WorldUnmanaged.Name;

        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        networkIdFromEntity.Update(ref state);

        foreach (var (id, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequest>().WithEntityAccess())
        {
            commandBuffer.AddComponent<NetworkStreamInGame>(id.ValueRO.SourceConnection);
            NetworkId networkId = networkIdFromEntity[id.ValueRO.SourceConnection];
            Debug.Log($"{networkId.Value} connected to {worldName}");
            //ServerConsole.Log(ServerConsole.LogType.Info, $"{networkId.Value} connected to {worldName}");
            commandBuffer.DestroyEntity(entity);
        }
        commandBuffer.Playback(state.EntityManager);
        //commandBuffer.Dispose();
    }
}
