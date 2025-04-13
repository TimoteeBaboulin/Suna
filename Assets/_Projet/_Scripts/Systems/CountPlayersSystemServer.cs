using Unity.Burst;
using Unity.Entities;
using UnityEngine;

//[UpdateBefore(typeof(RoundSystemServer))]
//[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(RespawnSystem))]
public partial class CountPlayersSystemServer : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();

        RequireForUpdate<RoundComponent>();
        RequireForUpdate<CharacterIsEnable>();
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

        if (World.IsCreated)
        {
            playersAliveRW.ValueRW.nativePlayersAlive = PlayerHelpers.CountPlayersAliveManaged(TeamSideType.Natif, World);
            playersAliveRW.ValueRW.corpoPlayersAlive = PlayerHelpers.CountPlayersAliveManaged(TeamSideType.Corpo, World);
        }
    }
}