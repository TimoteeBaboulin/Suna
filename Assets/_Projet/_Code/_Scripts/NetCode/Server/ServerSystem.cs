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
        EntityCommandBuffer command = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<InitializedClient>().WithEntityAccess())
        {
            command.AddComponent<InitializedClient>(entity);
            ServerConsole.Log(ServerConsole.LogType.Info, $"Client with id : {id.ValueRO}, connected to {worldName}");
        }
        command.Playback(EntityManager);
        command.Dispose();
    }
}
