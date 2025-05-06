using GameNetwork.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct ChooseTeamRpc : IRpcCommand
{
    public TeamSideType team;
    public Entity clientEntity;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct TeamChoiceSystemServer : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (command, rpc, rpcEntity) in
        SystemAPI.Query<ReceiveRpcCommandRequest, ChooseTeamRpc>()
        .WithEntityAccess())
        {
            ecb.DestroyEntity(rpcEntity);

            ClientComponent client = SystemAPI.GetComponent<ClientComponent>(rpc.clientEntity);
            int networkId = client.networkID;
            TeamSideType team = PlayerHelpers.AssignTeamToPlayer(PlayerHelpers.FindCurrentPlayerForNetworkId(networkId), rpc.team);
            client.team = team;

            
            SystemAPI.SetComponent(rpc.clientEntity, client);

            var hostSession = ClientTransportHelper.instance.Session.AsHost();
            hostSession.SavePlayerDataAsync(client.playerID.ToString());
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
