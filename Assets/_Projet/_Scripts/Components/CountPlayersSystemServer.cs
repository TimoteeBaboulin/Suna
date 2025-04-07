using Unity.Burst;
using Unity.Entities;

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

        RefRW<PlayerCounts> playersAliveRW = SystemAPI.GetComponentRW<PlayerCounts>(entity);

        playersAliveRW.ValueRW.nativePlayersAlive = PlayerHelpers.CountPlayersAliveManaged(TeamSideType.Natif, World);
        playersAliveRW.ValueRW.corpoPlayersAlive = PlayerHelpers.CountPlayersAliveManaged(TeamSideType.Corpo, World);
    }
}