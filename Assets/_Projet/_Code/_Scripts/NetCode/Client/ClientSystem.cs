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

public struct SpawnUnitRpcCommand : IRpcCommand
{

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
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ServerMessageRpcCommand>>().WithEntityAccess())
        {
            ServerConsole.Log(ServerConsole.LogType.Info, $"message to client {command.ValueRO.message}");
            commandBuffer.DestroyEntity(entity);
        }

        UpdatePlayerCamera();

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SendMessageRpc("Hello world", ConnectionManager.Instance.Client);
        }

        if ((Keyboard.current.vKey.wasPressedThisFrame))
        {
            SpawnUnitRpc(ConnectionManager.Instance.Client);
            Debug.Log("W pressed");
        }
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
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

    public void SpawnUnitRpc(World world)
    {
        if (world == null || !world.IsCreated)
        {
            return;
        }
        world.EntityManager.CreateEntity(typeof(SendRpcCommandRequest), typeof(SpawnUnitRpcCommand));
    }

    #region PrivateMethods

    private void UpdatePlayerCamera()
    {
        foreach (RefRO<LocalTransform> transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<GhostOwnerIsLocal>())
        {
            float3 position = transform.ValueRO.Position;
            quaternion rotation = transform.ValueRO.Rotation;

            Camera.main.transform.position = position;
            Camera.main.transform.rotation = rotation;
        }
    }
    #endregion
}
