//using Unity.Burst;
//using Unity.Entities;
//using Unity.NetCode;
//using UnityEngine;

//partial struct RpcLoggerSystem : ISystem
//{
//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
        
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        foreach ( var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ServerMessageRpcCommand>>().WithEntityAccess())
//        {
//            Debug.Log(command.ValueRO.message);
//        }
//    }

//    [BurstCompile]
//    public void OnDestroy(ref SystemState state)
//    {
        
//    }
//}
