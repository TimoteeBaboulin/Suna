using GameNetwork;
using GameNetwork.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Multiplay;
using Unity.Services.Multiplayer;
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
    private SessionTransportHelper serverSession;

    protected override void OnCreate()
    {
        _clients = GetComponentLookup<NetworkId>(true);
        RequireForUpdate<NetworkId>();
    }
    protected override void OnUpdate()
    {
        _clients.Update(this);

        //if (SessionData.Instance.CurrentPlayerCount < SessionData.Instance.SessionMaxPlayers)
        //{
        //    Debug.Log($"[ServerSystem] Session is not full; waiting for more clients = {SessionData.Instance.CurrentPlayerCount}/{SessionData.Instance.SessionMaxPlayers}");
        //    return;
        //}

        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        //Message from all clients to server
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ClientMessageRpcCommand>>().WithEntityAccess())
        {
            ServerConsole.Log(ServerConsole.LogType.Info, $"{command.ValueRO.message} from client index {request.ValueRO.SourceConnection.Index}, version {request.ValueRO.SourceConnection.Version}");
            Debug.Log($"{command.ValueRO.message} from client index {request.ValueRO.SourceConnection.Index}");
            commandBuffer.DestroyEntity(entity);
        }

        foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                                                         .WithNone<InitializedClient>()
                                                         .WithEntityAccess())
        {
            InstantiateClient(entity, commandBuffer);        
           // Debug.Log($"Init cient : {SessionData.Instance.CurrentPlayerCount}/{SessionData.Instance.SessionMaxPlayers}");
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
        //if (SystemAPI.TryGetSingleton(out PrefabsData prefabManager))
        //{
        //    if (prefabManager.Client == null)
        //    {
        //        return;
        //    }

        //    NetworkId networkId = SystemAPI.GetComponent<NetworkId>(ownerEntity);
        //    FixedString128Bytes worldName = ClientServerBootstrap.ServerWorld.Name;
        //    Entity client = ecb.Instantiate(prefabManager.Client);
        //    ecb.SetComponent(client, new GhostOwner() //Set owner of player to connection
        //    {
        //        NetworkId = networkId.Value
        //    });
        //    ecb.AppendToBuffer(ownerEntity, new LinkedEntityGroup() //Link it to connection
        //    {
        //        Value = client
        //    });

        //    ecb.AddComponent<InitializedClient>(ownerEntity);

        //    ServerConsole.Log(ServerConsole.LogType.Info, $"New Client connected with NetworkId {networkId.Value}, in the world {worldName}");
        //}

        if (SystemAPI.TryGetSingleton(out PrefabsData prefabManager))
        {
            if (prefabManager.Client == null)
                return;

            // Get the NetworkId from the connection entity.
            NetworkId networkId = SystemAPI.GetComponent<NetworkId>(ownerEntity);
            FixedString128Bytes worldName = ClientServerBootstrap.ServerWorld.Name;
            Entity client = ecb.Instantiate(prefabManager.Client);
            ecb.SetComponent(client, new GhostOwner() { NetworkId = networkId.Value });
            ecb.AppendToBuffer(ownerEntity, new LinkedEntityGroup() { Value = client });
            ecb.AddComponent<InitializedClient>(ownerEntity);

            Debug.Log($"[ServerSystem] Instantiated client ghost for connection {networkId.Value} in world {worldName}");
        }
    }
    #endregion
}