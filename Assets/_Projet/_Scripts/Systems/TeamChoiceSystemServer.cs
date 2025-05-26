using GameNetwork.Utils;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Rendering;

public struct ChooseTeamRpc : IRpcCommand
{
    public TeamSideType team;
    public int networkID;
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

            Entity correctEntity = Entity.Null;
            EntityQuery query = new EntityQueryBuilder(allocator: Allocator.Temp).WithAll<ClientComponent, GhostOwner>().Build(ref state);
            var clients = query.ToEntityArray(Allocator.Temp);
            var clientComponents = query.ToComponentDataArray<ClientComponent>(Allocator.Temp);

            for (int i = 0; i < clients.Length; i++)
            {
                var ghostOwner = SystemAPI.GetComponent<GhostOwner>(clients[i]);
                if (ghostOwner.NetworkId == rpc.networkID)
                {
                    correctEntity = clients[i];
                    break;
                }
            }

            if (correctEntity == Entity.Null)
            {
                Debug.LogWarning("No entity found matching the networkID.");
                continue;
            }

            ClientComponent client = SystemAPI.GetComponent<ClientComponent>(correctEntity);

            TeamSideType team = PlayerHelpers.AssignTeamToPlayer(PlayerHelpers.FindCurrentPlayerForNetworkId(client.networkID), rpc.team);
            if (team == TeamSideType.Neutre)
            {
                Debug.Log("Couldn't change teams");
                continue;
            }

            if (team == client.team)
            {
                continue;
            }

            client.team = team;

            Entity characterEntity = SystemAPI.GetComponent<ClientCharacterAttached>(correctEntity).Value;
            if (state.EntityManager.Exists(characterEntity))
            {
                ecb.DestroyEntity(characterEntity);
                ecb.AddComponent<WaitForRespawnTag>(correctEntity);
            }
            SystemAPI.SetComponent(correctEntity, client);

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
