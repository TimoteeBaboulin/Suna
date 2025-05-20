using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


 public struct MessageToServer : IRpcCommand
{
    public FixedString32Bytes word;
}


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

            //TODO: Handle changing team by removing the existing entities and creating a new one
            //TODO: Potentially link this to the isRelease variable (check with Game Manager first)
            //Uncomment this if you don't want to be able to change team
            //if (client.team != TeamSideType.Neutre)
            //    continue;

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

                //TODO: Remove this if it works
                //var command = new MessageToServer { word = "Corpo asked to spawn" };
                //RpcUtils.SendClientToServerRpc(ref command);
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
