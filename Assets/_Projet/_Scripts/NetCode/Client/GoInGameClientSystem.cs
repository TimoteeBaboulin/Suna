using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;

public struct GoInGameCommand : IRpcCommand
{

}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | (WorldSystemFilterFlags.ThinClientSimulation))]
public partial struct GoInGameClientSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAny<NetworkId>();
        builder.WithNone<NetworkStreamInGame>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (sceneRef, entity) in SystemAPI.Query<RefRO<SceneReference>>().WithEntityAccess())
        {
            if (!SceneSystem.IsSceneLoaded(state.WorldUnmanaged, entity))
            {
                return;
            }
        }

        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
        {
            commandBuffer.AddComponent<NetworkStreamInGame>(entity);
            Entity request = commandBuffer.CreateEntity();
            commandBuffer.AddComponent<GoInGameCommand>(request);
            commandBuffer.AddComponent<SendRpcCommandRequest>(request);
        }

        commandBuffer.Playback(state.EntityManager);
        commandBuffer.Dispose();
    }
}
