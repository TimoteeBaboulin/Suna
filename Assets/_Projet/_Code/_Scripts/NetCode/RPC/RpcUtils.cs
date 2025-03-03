using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.LightTransport;

public class RpcUtils
{
    //public static void SendServerRpc(EntityCommandBuffer ecb, string dataValue, Entity connectionEntity = default)
    //{
    //    var entity = ecb.CreateEntity();
    //    ecb.AddComponent(entity, new ServerRpcCommand { message = dataValue });
    //    ecb.AddComponent(entity, new SendRpcCommandRequest { TargetConnection = connectionEntity });
    //}

    public static void SendServerToClientRpc<T>(ref T command, Entity target = default) where T : unmanaged, IRpcCommand
    {
        World world = ClientServerBootstrap.ServerWorld;
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


    public static void SendClientToServerRpc<T>(ref T command) where T : unmanaged, IRpcCommand
    {
        World world = ClientServerBootstrap.ClientWorld;
        if (world == null || !world.IsCreated)
        {
            return;
        }
        Entity entity = world.EntityManager.CreateEntity(typeof(SendRpcCommandRequest), typeof(T));
        world.EntityManager.SetComponentData(entity, command);
    }
}
