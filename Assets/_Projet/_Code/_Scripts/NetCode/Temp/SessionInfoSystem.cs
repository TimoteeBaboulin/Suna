//using GameNetwork;
//using Unity.Entities;

//[UpdateInGroup(typeof(SimulationSystemGroup))]
//public partial class SessionInfoSystem : SystemBase
//{
//    protected override void OnCreate()
//    {
//        // Create a SessionInfo entity if one doesn't exist.
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
//}