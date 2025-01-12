using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine.InputSystem;

public struct ClientMessageRpcCommand : IRpcCommand
{
    public FixedString64Bytes message;
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>(); //Only update if there's a client with a network ID
    }
    protected override void OnUpdate()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SendMessageRpc("Hello world", ConnectionManager.Instance.Client);
        }
    }

    public void SendMessageRpc(string text, World world)
    {
        if (world == null || !world.IsCreated)
        {
            return;
        }
        Entity entity = world.EntityManager.CreateEntity(typeof(SendRpcCommandRequest), typeof(ClientMessageRpcCommand));
        world.EntityManager.SetComponentData(entity, new ClientMessageRpcCommand()
        {
            message = text
        });

    }
}
