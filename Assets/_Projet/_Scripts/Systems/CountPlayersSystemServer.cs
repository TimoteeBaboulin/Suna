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
//[UpdateBefore(typeof(RoundSystemServer))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateAfter(typeof(RespawnSystem))]
public partial class CountPlayersSystemServer : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();

        RequireForUpdate<RoundComponent>();
        //RequireForUpdate<CharacterIsEnable>();
    }

    protected override void OnUpdate()
    {
        if (!SystemAPI.TryGetSingletonEntity<RoundComponent>(out var entity))
        {
            return;
        }

        if (!SystemAPI.HasComponent<PlayerCounts>(entity))
        {
            return;
        }

        //if (!SystemAPI.HasComponent<CharacterIsEnable>(entity))
        //{
        //    return;
        //}
        RefRW<PlayerCounts> playersAliveRW = SystemAPI.GetComponentRW<PlayerCounts>(entity);

        playersAliveRW.ValueRW.nativePlayersAlive = PlayerHelpers.CountPlayersAliveManaged(TeamSideType.Natif, World);
        playersAliveRW.ValueRW.corpoPlayersAlive = PlayerHelpers.CountPlayersAliveManaged(TeamSideType.Corpo, World);
        SendTeamAliveCountsToClients(playersAliveRW.ValueRW.nativePlayersAlive, playersAliveRW.ValueRW.corpoPlayersAlive);
    }

    private void SendTeamAliveCountsToClients(int nativePlayersAlive, int corpoPlayersAlive)
    {
        var command = new TeamAliveCountRpc
        {
            nativePlayersAlive = nativePlayersAlive,
            corpoPlayersAlive = corpoPlayersAlive
        };

        RpcUtils.SendServerToClientRpc(ref command);  
    }
}