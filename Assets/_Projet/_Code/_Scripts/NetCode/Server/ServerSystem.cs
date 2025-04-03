
using GameNetwork.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public struct ServerMessageRpcCommand : IRpcCommand
{
    public FixedString64Bytes message;
}
public struct InitializedClient : IComponentData
{
    public int id;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ServerSystem : SystemBase
{
    private ComponentLookup<NetworkId> _clients;

    protected override async void OnCreate()
    {
        _clients = GetComponentLookup<NetworkId>(true);

        RequireForUpdate<NetworkId>();
    }
    protected override void OnUpdate()
    {
        _clients.Update(this);

        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        //Message from all clients to server
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ClientMessageRpcCommand>>().WithEntityAccess())
        {
            ServerConsole.Log(ServerConsole.LogType.Info, $"{command.ValueRO.message} from client index {request.ValueRO.SourceConnection.Index}, version {request.ValueRO.SourceConnection.Version}");
            Debug.Log($"{command.ValueRO.message} from client index {request.ValueRO.SourceConnection.Index}");
            commandBuffer.DestroyEntity(entity);
        }

        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<InitializedClient>().WithEntityAccess())
        {
            InstantiateClient(entity, commandBuffer);
        }

        //if (Keyboard.current.oKey.wasPressedThisFrame)
        //{
        //    ServerMessageRpcCommand command = new ServerMessageRpcCommand() { message = "Hello world" };
        //    RpcUtils.SendServerToClientRpc(ref command);
        //}

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }

    #region Public Methods

    public void InstantiateClient(Entity ownerEntity, EntityCommandBuffer ecb)
    {
        if (SystemAPI.TryGetSingleton(out ClientPrefabData prefabManager))
        {
            if (prefabManager.Client == null)
            {
                return;
            }

            NetworkId networkId = SystemAPI.GetComponent<NetworkId>(ownerEntity);
            FixedString128Bytes worldName = ClientServerBootstrap.ServerWorld.Name;
            Entity client = ecb.Instantiate(prefabManager.Client);
            ecb.SetComponent(client, new GhostOwner() //Set owner of player to connection
            {
                NetworkId = networkId.Value
            });
            ecb.AppendToBuffer(ownerEntity, new LinkedEntityGroup() //Link it to connection
            {
                Value = client
            });

            ecb.AddComponent<InitializedClient>(ownerEntity);

            ServerConsole.Log(ServerConsole.LogType.Info, $"New Client connected with NetworkId {networkId.Value}, in the world {worldName}");
        }
    }
    #endregion

    //Broadcast message to a target/client or to all clients if no target
    public void SendMessageRpc<T>(World world, ref T command, Entity target = default) where T : unmanaged, IRpcCommand
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

    #region Private Methods

    private void UpdateClient(ref EntityCommandBuffer commandBuffer, ref FixedString128Bytes worldName)
    {
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<InitializedClient>().WithEntityAccess())
        {
            commandBuffer.AddComponent<InitializedClient>(entity);
            ClientPrefabData prefabManager = SystemAPI.GetSingleton<ClientPrefabData>();

            //Instantiate player at connection
            if (prefabManager.Client != null)
            {
                Entity client = commandBuffer.Instantiate(prefabManager.Client);
                LocalTransform clientTransform = prefabManager.TransformCompData;

                commandBuffer.SetComponent(client, new LocalTransform() //Set position
                {
                    Position = clientTransform.Position,
                    Rotation = clientTransform.Rotation,
                    Scale = 1.0f
                });
                commandBuffer.SetComponent(client, new GhostOwner() //Set owner of player to connection
                {
                    NetworkId = id.ValueRO.Value,
                });
                commandBuffer.AppendToBuffer(entity, new LinkedEntityGroup() //Link it to connection
                {
                    Value = client
                });
            }
            ServerConsole.Log(ServerConsole.LogType.Info, $"Client with id : {id.ValueRO}, connected to {worldName}");
        }
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
    #endregion
}