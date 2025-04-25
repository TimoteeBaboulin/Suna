using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Services.Multiplayer;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct PlayerInPartyISystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Create Entity with Corpo Players Data in it
        NativeList<FixedString32Bytes> corpoPlayersNetworkId = new(Allocator.Temp);
        foreach (IReadOnlyPlayer player in PlayerHelpers.GetPlayersByTeam(TeamSideType.Corpo))
        {
            corpoPlayersNetworkId.Add(player.Id);
        }
        Entity corpoEntity = state.EntityManager.CreateEntity(typeof(PlayersInParty));
        state.EntityManager.AddComponentData<PlayersInParty>(corpoEntity, new()
        {
            PlayersTeam = (int)TeamSideType.Corpo,
            PlayersNetworkId = corpoPlayersNetworkId
        });

        // Create Entity with Natif Players Data in it
        NativeList<FixedString32Bytes> natifPlayersNetworkId = new(Allocator.Temp);
        foreach (IReadOnlyPlayer player in PlayerHelpers.GetPlayersByTeam(TeamSideType.Natif))
        {
            natifPlayersNetworkId.Add(player.Id);
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
    public NativeList<FixedString32Bytes> PlayersNetworkId;
}
