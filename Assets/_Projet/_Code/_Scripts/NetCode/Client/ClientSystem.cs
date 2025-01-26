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
        float dt = SystemAPI.Time.DeltaTime;
        foreach (RefRO<CameraAttachComponent> cameraAttach in SystemAPI.Query<RefRO<CameraAttachComponent>>().WithAll<GhostOwnerIsLocal>())
        {
            //Camera.main.transform.position = cameraAttach.ValueRO.transform.Position;
            //Camera.main.transform.rotation = cameraAttach.ValueRO.transform.Rotation;

            float3 targetPosition = cameraAttach.ValueRO.transform.Position;
            quaternion targetRotation = cameraAttach.ValueRO.transform.Rotation;

            Camera.main.transform.position = Vector3.Lerp(
                Camera.main.transform.position,
                targetPosition,
               dt * 20f
            );

            Camera.main.transform.rotation = Quaternion.Slerp(
                Camera.main.transform.rotation,
                targetRotation,
                dt * 20f
            );
        }
    }
    #endregion
}
