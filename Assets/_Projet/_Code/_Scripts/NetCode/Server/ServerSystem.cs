using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.InputSystem;

public struct ServerMessageRpcCommand : IRpcCommand
{
    public FixedString128Bytes message;
}
public struct InitializedClient : IComponentData
{

}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ServerSystem : SystemBase
{
    private ComponentLookup<NetworkId> _clients;

    protected override void OnCreate()
    {
        _clients = GetComponentLookup<NetworkId>(true);
    }
    protected override void OnUpdate()
    {
        _clients.Update(this);

        FixedString128Bytes worldName = ConnectionManager.Instance.Server.Name;
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        //Message from all clients to server
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ClientMessageRpcCommand>>().WithEntityAccess())
        {
            ServerConsole.Log(ServerConsole.LogType.Info, $"{command.ValueRO.message} from client index {request.ValueRO.SourceConnection.Index}, version {request.ValueRO.SourceConnection.Version}");
            commandBuffer.DestroyEntity(entity);
        }

        //Handle playerTemp prefab from client to server
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<SpawnUnitRpcCommand>>().WithEntityAccess())
        {
            PrefabsData prefabs;
            if (SystemAPI.TryGetSingleton<PrefabsData>(out prefabs) && prefabs.unit != null)
            {
                Entity unit = commandBuffer.Instantiate(prefabs.unit);
                commandBuffer.SetComponent(unit, new LocalTransform()
                {
                    Position = new float3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f)),
                    Rotation = Quaternion.identity,
                    Scale = 1.0f
                });

                //Set owner of prefabs to client otherwise server is considered the owner
                NetworkId networkId = _clients[request.ValueRO.SourceConnection];
                commandBuffer.SetComponent(unit, new GhostOwner()
                {
                    NetworkId = networkId.Value
                });

                //Link the units with the connection, if the connection is destroyed, destroy the unit as well
                commandBuffer.AppendToBuffer(request.ValueRO.SourceConnection, new LinkedEntityGroup()
                {
                    Value = unit
                });

                commandBuffer.DestroyEntity(entity);
            }
        }
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<InitializedClient>().WithEntityAccess())
        {
            commandBuffer.AddComponent<InitializedClient>(entity);
            PrefabsData prefabManager = SystemAPI.GetSingleton<PrefabsData>();

            //Instantiate player at connection
            if (prefabManager.player != null)
            {
                Entity player = commandBuffer.Instantiate(prefabManager.player);
                commandBuffer.SetComponent(player, new LocalTransform() //Set position
                {
                    Position = new float3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f)),
                    Rotation = Quaternion.identity,
                    Scale = 1.0f
                });
                commandBuffer.SetComponent(player, new GhostOwner() //Set owner of player to connection
                {
                    NetworkId = id.ValueRO.Value
                });
                commandBuffer.AppendToBuffer(entity, new LinkedEntityGroup() //Link it to connection
                {
                    Value = player
                });
            }
            ServerConsole.Log(ServerConsole.LogType.Info, $"Client with id : {id.ValueRO}, connected to {worldName}");
        }

        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            ServerMessageRpcCommand command = new ServerMessageRpcCommand() { message = "Hello world" };

            SendMessageRpc("Hello world", ConnectionManager.Instance.Server, ref command);
        }
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }

    //Broadcast message to a target/client or to all clients if no target
    public void SendMessageRpc<T>(string text, World world, ref T command, Entity target = default) where T : unmanaged, IRpcCommand
    {
        if (world == null || !world.IsCreated)
        {
            return;
        }

        Entity entity = world.EntityManager.CreateEntity(typeof(SendRpcCommandRequest), typeof(T));
        world.EntityManager.SetComponentData(entity, command);
        

        if (target != Entity.Null)
        {
            world.EntityManager.SetComponentData(entity, new SendRpcCommandRequest()
            {
                TargetConnection = target
            });
        }
    }
}
