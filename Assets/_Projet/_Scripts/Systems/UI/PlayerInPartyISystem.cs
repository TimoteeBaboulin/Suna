using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Services.Multiplayer;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct PlayerInPartyISystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Create Entity with Corpo Players Data in it
        NativeList<FixedString64Bytes> corpoPlayersNetworkId = new(Allocator.Temp);
        foreach (ClientComponent player in PlayerHelpers.GetClientPlayersByTeam(TeamSideType.Corpo))
        {
            corpoPlayersNetworkId.Add(player.playerID);
        }
        Entity corpoEntity = state.EntityManager.CreateEntity(typeof(PlayersInParty));
        state.EntityManager.AddComponentData<PlayersInParty>(corpoEntity, new()
        {
            PlayersTeam = (int)TeamSideType.Corpo,
            PlayersNetworkId = corpoPlayersNetworkId
        });

        // Create Entity with Natif Players Data in it
        NativeList<FixedString64Bytes> natifPlayersNetworkId = new(Allocator.Temp);
        foreach (ClientComponent player in PlayerHelpers.GetClientPlayersByTeam(TeamSideType.Natif))
        {
            natifPlayersNetworkId.Add(player.playerID);
        }
        Entity natifEntity = state.EntityManager.CreateEntity(typeof(PlayersInParty));
        state.EntityManager.AddComponentData<PlayersInParty>(natifEntity, new()
        {
            PlayersTeam = (int)TeamSideType.Natif,
            PlayersNetworkId = natifPlayersNetworkId
        });
    }
}

public struct PlayersInParty : IComponentData
{
    public int PlayersTeam;
    public NativeList<FixedString64Bytes> PlayersNetworkId;
}
