using GameNetwork;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;
using Unity.Services.Multiplayer;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class SessionInfoSystem : SystemBase
{
    //    protected override void OnCreate()
    //    {
    //        if (!SystemAPI.HasSingleton<SessionInfo>())
    //        {
    //            Entity sessionEntity = EntityManager.CreateEntity(typeof(SessionInfo));
    //            string initialSessionID = string.IsNullOrEmpty(SessionData.Instance.SessionID)
    //                ? "SESSION01"
    //                : SessionData.Instance.SessionID;
    //            EntityManager.SetComponentData(sessionEntity, new SessionInfo
    //            {
    //                SessionID = initialSessionID,
    //                PlayerCount = SessionData.Instance.CurrentPlayerCount,
    //                MaxPlayers = SessionData.Instance.SessionMaxPlayers
    //            });
    //        }
    //    }

    //    protected override void OnUpdate()
    //    {
    //        if (SystemAPI.HasSingleton<SessionInfo>())
    //        {
    //            SessionInfo sessionInfo = SystemAPI.GetSingleton<SessionInfo>();
    //            sessionInfo.SessionID = SessionData.Instance.SessionID;
    //            sessionInfo.PlayerCount = SessionData.Instance.CurrentPlayerCount;
    //            sessionInfo.MaxPlayers = SessionData.Instance.SessionMaxPlayers;
    //            SystemAPI.SetSingleton(sessionInfo);
    //        }
    //    }

    //protected override void OnCreate()
    //{
    //    var serverConfig = MultiplayService.Instance.ServerConfig;
    //    Debug.Log($"Server ID[{serverConfig.ServerId}]");
    //    Debug.Log($"AllocationID[{serverConfig.AllocationId}]");
    //    Debug.Log($"Port[{serverConfig.Port}]");
    //    Debug.Log($"QueryPort[{serverConfig.QueryPort}");
    //    Debug.Log($"LogDirectory[{serverConfig.ServerLogDirectory}]");
    //}
    protected override async void OnUpdate()
    {
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ClientSessionCreationCommand>>().WithEntityAccess())
        {
            Debug.Log($"Command received value : {command.ValueRO.createNewSession}");
            if (SystemAPI.HasSingleton<ConnectionInfo>())
            {
                ConnectionInfo connectionInfo = SystemAPI.GetSingleton<ConnectionInfo>();
                Debug.Log($"connectionInfo : {connectionInfo.IP.ToString()} , {connectionInfo.Port} , {connectionInfo.IsClientLocal}");
                if (command.ValueRO.createNewSession)
                {
                    Debug.Log(command.ValueRO.createNewSession);
                    await ServerSessionFactory.CreateServerSession(connectionInfo.IP.ToString(), connectionInfo.Port, connectionInfo.IsClientLocal);
                }
            }
        }
    }
}