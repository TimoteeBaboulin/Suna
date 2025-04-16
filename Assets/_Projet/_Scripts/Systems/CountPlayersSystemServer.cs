using GameNetwork.Utils;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct TeamAliveCountRpc : IRpcCommand
{
    public int nativePlayersAlive;
    public int corpoPlayersAlive;
}
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateAfter(typeof(RoundComponent))]
public partial class CountPlayersSystemServer : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();

        RequireForUpdate<RoundComponent>();
    }

    protected override void OnUpdate()
    {
        if (!SystemAPI.TryGetSingletonEntity<RoundComponent>(out var entity))
        {
            return;
        }

        if (!SystemAPI.HasComponent<PlayerAliveCounts>(entity))
        {
            return;
        }

        // Get the alive counts using our new helper.
        PlayerHelpers.AliveCounts counts = PlayerHelpers.GetCurrentAliveCounts(World);

        RefRW<PlayerAliveCounts> playersAliveRW = SystemAPI.GetComponentRW<PlayerAliveCounts>(entity);
        playersAliveRW.ValueRW.corpoPlayersAlive = counts.corpoPlayersAlive;
        playersAliveRW.ValueRW.nativePlayersAlive = counts.natifPlayersAlive;
        //  SendTeamAliveCountsToClients(playersAliveRW.ValueRW.nativePlayersAlive, playersAliveRW.ValueRW.corpoPlayersAlive);
    }

    //private void SendTeamAliveCountsToClients(int nativePlayersAlive, int corpoPlayersAlive)
    //{
    //    var command = new TeamAliveCountRpc
    //    {
    //        nativePlayersAlive = nativePlayersAlive,
    //        corpoPlayersAlive = corpoPlayersAlive
    //    };

    //    RpcUtils.SendServerToClientRpc(ref command);  
    //}
}