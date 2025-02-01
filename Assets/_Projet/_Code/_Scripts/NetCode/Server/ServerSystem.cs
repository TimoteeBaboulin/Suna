using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using System.ComponentModel;
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

        //Handle playerTemp prefab from client to server
        //foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<SpawnUnitRpcCommand>>().WithEntityAccess())
        //{
        //    PrefabsData prefabs;
        //    if (SystemAPI.TryGetSingleton<PrefabsData>(out prefabs) && prefabs.unit != null)
        //    {
        //        Entity unit = commandBuffer.Instantiate(prefabs.unit);
        //        commandBuffer.SetComponent(unit, new LocalTransform()
        //        {
        //            Position = new float3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f)),
        //            Rotation = Quaternion.identity,
        //            Scale = 1.0f
        //        });

        //        //Set owner of prefabs to client otherwise server is considered the owner
        //        NetworkId networkId = _clients[request.ValueRO.SourceConnection];
        //        commandBuffer.SetComponent(unit, new GhostOwner()
        //        {
        //            NetworkId = networkId.Value
        //        });

        //        //Link the units with the connection, if the connection is destroyed, destroy the unit as well
        //        commandBuffer.AppendToBuffer(request.ValueRO.SourceConnection, new LinkedEntityGroup()
        //        {
        //            Value = unit
        //        });

        //        commandBuffer.DestroyEntity(entity);
        //    }
        //}



        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<InitializedClient>().WithEntityAccess())
        {
            commandBuffer.AddComponent<InitializedClient>(entity);

            PrefabsData prefabManager = SystemAPI.GetSingleton<PrefabsData>();
            InstantiatePlayer(entity, commandBuffer);
            //SpawnPlayer(entity, commandBuffer, prefabManager.transformCompData.Position);
        }

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }

    #region Public Methods

    public void InstantiatePlayer(Entity ownerEntity, EntityCommandBuffer ecb)
    {
        PrefabsData prefabManager = SystemAPI.GetSingleton<PrefabsData>();
        if (prefabManager.player == null)
        {
            return;
        }

        NetworkId networkId = SystemAPI.GetComponent<NetworkId>(ownerEntity);
        FixedString128Bytes worldName = ConnectionManager.Instance.Server.Name;

        Entity player = ecb.Instantiate(prefabManager.player);
        ecb.SetComponent(player, new GhostOwner() //Set owner of player to connection
        {
            NetworkId = networkId.Value
        });
        ecb.AppendToBuffer(ownerEntity, new LinkedEntityGroup() //Link it to connection
        {
            Value = player
        });

        ServerConsole.Log(ServerConsole.LogType.Info, $"New Player connected with NetworkId {networkId.Value}, in the world {worldName}");
    }

    //public void SpawnCharacter(Entity ownerEntity, EntityCommandBuffer ecb, float3 position)
    //{
    //    PrefabsData prefabManager = SystemAPI.GetSingleton<PrefabsData>();
    //    if (prefabManager.player == null)
    //    {
    //        ServerConsole.Log(ServerConsole.LogType.Error, $"Player prefab in Player Manager is null during SpawnPlayer execution");
    //        return;
    //    }

    //    NetworkId networkId = SystemAPI.GetComponent<NetworkId>(ownerEntity);
    //    FixedString128Bytes worldName = ConnectionManager.Instance.Server.Name;

    //    Entity player = ecb.Instantiate(prefabManager.player);
    //    ecb.SetComponent(player, new LocalTransform() //Set position
    //    {
    //        Position = position,
    //        Rotation = quaternion.identity,
    //        Scale = 1.0f
    //    });
    //    ecb.SetComponent(player, new GhostOwner() //Set owner of player to connection
    //    {
    //        NetworkId = networkId.Value
    //    });
    //    ecb.AppendToBuffer(ownerEntity, new LinkedEntityGroup() //Link it to connection
    //    {
    //        Value = player
    //    });

    //    ServerConsole.Log(ServerConsole.LogType.Info, $"Player spawned with NetworkId {networkId.Value}, in the world {worldName}");
    //}

    #endregion

    //Broadcast message to a target/client or to all clients if no target
    public void SendMessageRpc(string text, World world, Entity target = default)
    {
        if (world == null || !world.IsCreated)
        {
            return;
        }
        Entity entity = world.EntityManager.CreateEntity(typeof(SendRpcCommandRequest), typeof(ServerMessageRpcCommand));
        world.EntityManager.SetComponentData(entity, new ServerMessageRpcCommand()
        {
            message = text
        });

        if (target != Entity.Null)
        {
            world.EntityManager.SetComponentData(entity, new SendRpcCommandRequest()
            {
                TargetConnection = target
            });
        }

    }

    #region Private Methods

    private void UpdatePlayer(ref EntityCommandBuffer commandBuffer, ref FixedString128Bytes worldName)
    {
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<InitializedClient>().WithEntityAccess())
        {
            commandBuffer.AddComponent<InitializedClient>(entity);
            PrefabsData prefabManager = SystemAPI.GetSingleton<PrefabsData>();

            //Instantiate player at connection
            if (prefabManager.player != null)
            {
                Entity player = commandBuffer.Instantiate(prefabManager.player);
                LocalTransform playerTransform = prefabManager.transformCompData;

                commandBuffer.SetComponent(player, new LocalTransform() //Set position
                {
                    Position = playerTransform.Position,
                    Rotation = playerTransform.Rotation,
                    Scale = 1.0f
                });
                commandBuffer.SetComponent(player, new GhostOwner() //Set owner of player to connection
                {
                    NetworkId = id.ValueRO.Value,
                });
                commandBuffer.AppendToBuffer(entity, new LinkedEntityGroup() //Link it to connection
                {
                    Value = player
                });
            }
            ServerConsole.Log(ServerConsole.LogType.Info, $"Client with id : {id.ValueRO}, connected to {worldName}");
        }
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
    #endregion
}
