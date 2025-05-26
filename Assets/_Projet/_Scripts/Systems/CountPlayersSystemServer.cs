using GameNetwork.Utils;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


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

        PlayerHelpers.AliveCounts counts = PlayerHelpers.GetCurrentAliveCounts(World);
        RefRW<PlayerAliveCounts> playersAliveRW = SystemAPI.GetComponentRW<PlayerAliveCounts>(entity);
        playersAliveRW.ValueRW.corpoPlayersAlive = counts.corpoPlayersAlive;
        playersAliveRW.ValueRW.nativePlayersAlive = counts.natifPlayersAlive;
    }
}