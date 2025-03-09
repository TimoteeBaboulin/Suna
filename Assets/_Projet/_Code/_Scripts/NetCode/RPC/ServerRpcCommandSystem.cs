//using NUnit.Framework;
//using Unity.Burst;
//using Unity.Burst.Intrinsics;
//using Unity.Entities;
//using Unity.NetCode;
//using UnityEditor.PackageManager.Requests;

//[UpdateInGroup(typeof(RpcCommandRequestSystemGroup))]
//[CreateAfter(typeof(RpcSystem))]
//[BurstCompile]
//partial struct ServerRpcCommandSystem : ISystem
//{
//    RpcCommandRequest<ServerRpcCommand, ServerRpcCommand> _request;

//    [BurstCompile]
//    struct SendRpc : IJobChunk
//    {
//        public RpcCommandRequest<ServerRpcCommand, ServerRpcCommand>.SendRpcData data;
//        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
//        {
//            Assert.IsFalse(useEnabledMask);
//            data.Execute(chunk, unfilteredChunkIndex);
//        }
//    }

//    public void OnCreate(ref SystemState state)
//    {
//        _request.OnCreate(ref state);
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        SendRpc sendJob = new SendRpc
//        {
//            data = _request.InitJobData(ref state)
//        };
//        state.Dependency = sendJob.Schedule(_request.Query, state.Dependency);
//    }
//}


