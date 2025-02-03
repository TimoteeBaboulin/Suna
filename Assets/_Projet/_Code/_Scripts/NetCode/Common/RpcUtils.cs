using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class RpcUtils
{
    public static void SendServerRpc(EntityCommandBuffer ecb, string dataValue, Entity connectionEntity = default )
    {
        var entity = ecb.CreateEntity();
        ecb.AddComponent(entity, new ServerRpcCommand { message = dataValue });
        ecb.AddComponent(entity, new SendRpcCommandRequest { TargetConnection = connectionEntity });
    }
}
