using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[BurstCompile]
public struct ServerRpcCommand : IComponentData, IRpcCommandSerializer<ServerRpcCommand>
{
    public FixedString128Bytes message;
    public void Serialize(ref DataStreamWriter writer, in RpcSerializerState state, in ServerRpcCommand data)
    {
        writer.WriteFixedString128(data.message);
    }
    public void Deserialize(ref DataStreamReader reader, in RpcDeserializerState state, ref ServerRpcCommand data)
    {
        data.message = reader.ReadFixedString128();
    }


    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return InvokeExecuteFunctionPointer;
    }

    [BurstCompile(DisableDirectCall = true)]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        RpcExecutor.ExecuteCreateRequestComponent<ServerRpcCommand, ServerRpcCommand>(ref parameters);
    }

    static readonly PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
        new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
}

