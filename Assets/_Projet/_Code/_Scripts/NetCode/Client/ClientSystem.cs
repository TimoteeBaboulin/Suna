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

public struct ClientSessionCreationCommand : IRpcCommand
{
    public bool createNewSession;
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
    #region PrivateMethods

    #endregion
}


//[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ClientSimulation)]
//public partial class MainMenuSystem : SystemBase
//{
//    protected override void OnUpdate()
//    {
//        if (SystemAPI.HasSingleton<SessionInfo>())
//        {
//            Debug.Log($"SessionInfo ;{SystemAPI.HasSingleton<SessionInfo>()}");
//            SessionInfo sessionInfo = SystemAPI.GetSingleton<SessionInfo>();
//            // Convert FixedString64Bytes to string if needed:
//            string sessionID = sessionInfo.SessionID.ToString();

//            // Pass the sessionID to GameManager (assuming a singleton pattern)
//            if (GameManager.Instance != null)
//            {
//                GameManager.Instance.SetSessionID(sessionID);
//            }
//        }
//    }
//}


//[UpdateInGroup(typeof(InitializationSystemGroup))]
//[WorldSystemFilter(WorldSystemFilterFlags.Default)]
//public partial class CreateSessionInfoInDefaultWorldSystem : SystemBase
//{
//    protected override void OnCreate()
//    {
//        // If it doesn't exist, create it here in the Default World.
//        if (!SystemAPI.HasSingleton<SessionInfo>())
//        {
//            Entity e = EntityManager.CreateEntity(typeof(SessionInfo));

//            // Initialize with some fallback or known data
//            SessionInfo info = new SessionInfo
//            {
//                SessionID = "SESSION_DEFAULT",
//                PlayerCount = 0,
//                MaxPlayers = 1
//            };
//            EntityManager.SetComponentData(e, info);
//        }
//    }

//    protected override void OnUpdate()
//    {
//        // If you have new data, update it here.
//        // But remember, the server world won't automatically replicate
//        // to the Default World. You have to do it manually.
//    }
//}
