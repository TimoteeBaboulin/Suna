
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
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

    protected override void OnCreate()
    {
        _clients = GetComponentLookup<NetworkId>(true);

        RequireForUpdate<NetworkId>();
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

        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<InitializedClient>().WithEntityAccess())
        {
            InstantiateClient(entity, commandBuffer);
        }

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }

    #region Public Methods

    public void InstantiateClient(Entity ownerEntity, EntityCommandBuffer ecb)
    {
        if (SystemAPI.TryGetSingleton(out PrefabsData prefabManager))
        {
            prefabManager = SystemAPI.GetSingleton<PrefabsData>();

            if (prefabManager.client == null)
            {
                return;
            }

            NetworkId networkId = SystemAPI.GetComponent<NetworkId>(ownerEntity);
            FixedString128Bytes worldName = ConnectionManager.Instance.Server.Name;

            Entity client = ecb.Instantiate(prefabManager.client);
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

    #region Private Methods

    private void UpdateClient(ref EntityCommandBuffer commandBuffer, ref FixedString128Bytes worldName)
    {
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<InitializedClient>().WithEntityAccess())
        {
            commandBuffer.AddComponent<InitializedClient>(entity);
            PrefabsData prefabManager = SystemAPI.GetSingleton<PrefabsData>();

            //Instantiate player at connection
            if (prefabManager.client != null)
            {
                Entity client = commandBuffer.Instantiate(prefabManager.client);
                LocalTransform clientTransform = prefabManager.transformCompData;

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