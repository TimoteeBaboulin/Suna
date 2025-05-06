using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct TeamChoiceSystemClient : ISystem
{
    public void OnUpdate(ref SystemState state)
    {


        Debug.Log("sdmlfjsqmldfj 1");


        foreach (var (client, clientEntity) in
        SystemAPI.Query<ClientComponent>()
        .WithEntityAccess())
        {
            Debug.Log("sdmlfjsqmldfj 2");
            if (!SystemAPI.HasComponent<GhostOwnerIsLocal>(clientEntity))
                continue;

            Debug.Log("sdmlfjsqmldfj 3");

            if (client.team != TeamSideType.Neutre)
                continue;

            EntityQuery query = new EntityQueryBuilder(allocator: Allocator.Temp).WithAll<ClientComponent, GhostOwnerIsLocal>().Build(ref state);

            ChooseTeamRpc rpc = new ChooseTeamRpc
            {
                clientEntity = query.ToEntityArray(Allocator.Temp)[0]
            };

            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("Sending please make me a corpor rpc");
                rpc.team = TeamSideType.Corpo;
                RpcUtils.SendClientToServerRpc(ref rpc);
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                Debug.Log("Sending please make me a corpor rpc");
                rpc.team = TeamSideType.Natif;
                RpcUtils.SendClientToServerRpc(ref rpc);
            }
        }
    }
}
