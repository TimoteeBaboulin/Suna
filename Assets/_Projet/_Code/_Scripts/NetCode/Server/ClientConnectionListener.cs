using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial struct ClientConnectionListener : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<NetworkId>()
            .WithNone<NetworkStreamInGame>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess().WithNone<NetworkStreamInGame>())
        {
            commandBuffer.AddComponent<NetworkStreamInGame>(entity);
            var req = commandBuffer.CreateEntity();
            commandBuffer.AddComponent<GoInGameRequest>(req);
            commandBuffer.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
        }
        commandBuffer.Playback(state.EntityManager);
    }
}
