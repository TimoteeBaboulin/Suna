using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

public struct InitializedClient : IComponentData
{

}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ServerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        FixedString128Bytes worldName = ConnectionManager.Instance.Server.Name;
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        //Message from all clients
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ClientMessageRpcCommand>>().WithEntityAccess())
        {
            ServerConsole.Log(ServerConsole.LogType.Debug, $"{command.ValueRO.message} from client index {request.ValueRO.SourceConnection.Index}, version {request.ValueRO.SourceConnection.Version}");
            commandBuffer.DestroyEntity(entity);
        }

        //Message to all clients
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<InitializedClient>().WithEntityAccess())
        {
            commandBuffer.AddComponent<InitializedClient>(entity);
            ServerConsole.Log(ServerConsole.LogType.Info, $"Client with id : {id.ValueRO}, connected to {worldName}");
        }
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
}
