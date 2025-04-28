using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class PlayerIconsLinkSystem : SystemBase
{
    public class PlayersInPartyArgs : EventArgs
    {
        public List<string> PlayersNetworkId;
        public TeamSideType PlayersTeam;
    }
    public event EventHandler<PlayersInPartyArgs> PlayersInPartyEvent;

    [BurstCompile]
    protected override void OnUpdate()
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

        foreach (var (playersInParty, entity) in SystemAPI.Query<RefRO<PlayersInParty>>().WithEntityAccess())
        {
            List<string> playersNetworkId = new();
            for (int i = 0; i < playersInParty.ValueRO.PlayersNetworkId.Length; i++)
            {
                playersNetworkId.Add(playersInParty.ValueRO.PlayersNetworkId[i].ToString());
            }

            PlayersInPartyEvent?.Invoke(this, new PlayersInPartyArgs
            {
                PlayersNetworkId = playersNetworkId,
                PlayersTeam = (TeamSideType)playersInParty.ValueRO.PlayersTeam
            });

            ecb.DestroyEntity(entity);
        }
    }
}
