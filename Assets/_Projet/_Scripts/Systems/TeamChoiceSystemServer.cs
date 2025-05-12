using GameNetwork.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Rendering;

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
            Debug.Log("Receiving please make me a team player rpc");

            ecb.DestroyEntity(rpcEntity);

            ClientComponent client = SystemAPI.GetComponent<ClientComponent>(rpc.clientEntity);
            int networkId = client.networkID;
            TeamSideType team = PlayerHelpers.AssignTeamToPlayer(PlayerHelpers.FindCurrentPlayerForNetworkId(networkId), rpc.team);
            if (team == TeamSideType.Neutre)
            {
                Debug.Log("Couldn't change teams");
                continue;
            }

            if (team == client.team)
            {
                return;
            }

            client.team = team;

            Entity characterEntity = SystemAPI.GetComponent<ClientCharacterAttached>(rpc.clientEntity).Value;
            if (state.EntityManager.Exists(characterEntity))
            {
                ecb.DestroyEntity(characterEntity);
                ecb.AddComponent<WaitForRespawnTag>(rpc.clientEntity);
            }
            SystemAPI.SetComponent(rpc.clientEntity, client);

            var hostSession = ClientTransportHelper.instance.Session.AsHost();
            hostSession.SavePlayerDataAsync(client.playerID.ToString());
        }

        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<MessageToServer>>().WithEntityAccess())
        {
            ServerConsole.Log(ServerConsole.LogType.Info, $"{command.ValueRO.word} from client index" +
                $" {request.ValueRO.SourceConnection.Index}, version {request.ValueRO.SourceConnection.Version}");
            Debug.Log($"{command.ValueRO.word} from client index {request.ValueRO.SourceConnection.Index}");
            ecb.DestroyEntity(entity);
        }


        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
