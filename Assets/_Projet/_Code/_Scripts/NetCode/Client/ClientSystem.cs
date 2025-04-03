using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine.InputSystem;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.VisualScripting.Dependencies.NCalc;

public struct ClientMessageRpcCommand : IRpcCommand
{
    public FixedString64Bytes message;
}


[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ServerMessageRpcCommand>>().WithEntityAccess())
        {
            ServerConsole.Log(ServerConsole.LogType.Info, $"message to client {command.ValueRO.message}");
            Debug.Log($"message to client {command.ValueRO.message}");
            commandBuffer.DestroyEntity(entity);
        }

        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            ClientMessageRpcCommand command = new ClientMessageRpcCommand() { message = "Client message to server BOUMBOUMBOUBm" };
            RpcUtils.SendClientToServerRpc(ref command);
        }
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
}
