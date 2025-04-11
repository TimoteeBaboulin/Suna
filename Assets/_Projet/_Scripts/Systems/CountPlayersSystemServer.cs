using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[UpdateBefore(typeof(RoundSystemServer))]
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

        if (!SystemAPI.HasComponent<PlayerCounts>(entity))
        {
            return;
        }
        RefRW<PlayerCounts> playersAliveRW = SystemAPI.GetComponentRW<PlayerCounts>(entity);
	
	playersAliveRW.ValueRW.nativePlayersAlive = 1;
	playersAliveRW.ValueRW.corpoPlayersAlive = 1;

        //playersAliveRW.ValueRW.nativePlayersAlive = PlayerHelpers.CountPlayersAliveManaged(TeamSideType.Natif, World);
        //playersAliveRW.ValueRW.corpoPlayersAlive = PlayerHelpers.CountPlayersAliveManaged(TeamSideType.Corpo, World);
    }
}