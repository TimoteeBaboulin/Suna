using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct TeamChoiceSystemClient : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (client, clientEntity) in
        SystemAPI.Query<ClientComponent>()
        .WithEntityAccess())
        {
            if (!SystemAPI.HasComponent<GhostOwnerIsLocal>(clientEntity))
                continue;


            if (client.team != TeamSideType.Neutre)
                continue;

            EntityQuery query = new EntityQueryBuilder(allocator: Allocator.Temp).WithAll<ClientComponent, GhostOwnerIsLocal>().Build(ref state);

            ChooseTeamRpc rpc = new ChooseTeamRpc
            {
                clientEntity = query.ToEntityArray(Allocator.Temp)[0]
            };

            if (Input.GetKeyDown(KeyCode.C))
            {
                rpc.team = TeamSideType.Corpo;
                RpcUtils.SendClientToServerRpc(ref rpc);
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                rpc.team = TeamSideType.Natif;
                RpcUtils.SendClientToServerRpc(ref rpc);
            }
        }
    }
}
