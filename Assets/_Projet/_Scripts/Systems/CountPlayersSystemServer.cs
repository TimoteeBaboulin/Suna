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
        
        Debug.Log("[Debug] Start counting system");
        if (!SystemAPI.TryGetSingletonEntity<RoundComponent>(out var entity))
        {
            return;
        }

        PlayerCounts playersAlive = SystemAPI.GetComponent<PlayerCounts>(entity);

        
        playersAlive.nativePlayersAlive = PlayerHelpers.CountPlayersAliveManaged(TeamSideType.Natif, World);
        playersAlive.corpoPlayersAlive = PlayerHelpers.CountPlayersAliveManaged(TeamSideType.Corpo, World);

        SystemAPI.SetComponent(entity, playersAlive);
        Debug.Log("[Debug] Stop counting system");
    }
}