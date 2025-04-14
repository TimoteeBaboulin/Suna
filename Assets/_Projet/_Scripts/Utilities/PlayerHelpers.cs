using GameNetwork.Utils;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using UnityEngine;
using static Unity.NetCode.ClientServerBootstrap;

public static class PlayerHelpers
{
    /// <summary>
    /// Function used to receive the number of players alive in a team
    /// The function is managed so can't be used in ISystems or Burst Compiled methods
    /// Written by Timotee
    /// </summary>
    static public int CountPlayersAliveManaged(TeamSideType team, World world)
    {
        string teamName = team == TeamSideType.Corpo ? "Corpo" : "Natif";

        List<IReadOnlyPlayer> teamList = GetPlayersByTeam(teamName);
        if (teamList == null){return 0;}

        int aliveCount = 0;

        EntityQuery characterQuery = world.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(CharacterClientAttachedComponent), typeof(CharacterIsEnable) },
            Options = EntityQueryOptions.IgnoreComponentEnabledState
        });

        NativeArray<Entity> characterEntities = characterQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < characterEntities.Length; i++)
        {
            Entity characterEntity = characterEntities[i];
            var entityManager = world.EntityManager;

            if (!entityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
                continue;

            CharacterClientAttachedComponent attached = entityManager.GetComponentData<CharacterClientAttachedComponent>(characterEntity);
            Entity clientEntity = attached.ClientEntity;

            ClientComponent client = entityManager.GetComponentData<ClientComponent>(clientEntity);
            bool found = false;
            for (int j = 0; j < teamList.Count; j++)
            {
                var player = teamList[j];

                if (player.Id == client.playerID.ToString())
                {
                    found = true;
                    break; 
                }
            }

            if (found)
            {
                aliveCount++;
            }
        }
        characterEntities.Dispose();
        return aliveCount;
    }

    static public List<IReadOnlyPlayer> GetPlayersByTeam(string teamName)
    {
        var teamPlayers = new List<IReadOnlyPlayer>();
        if (ClientTransportHelper.instance != null)
        {
            var session = ClientTransportHelper.instance.Session;
            var players = session.Players;

            foreach (var player in players)
            {
                // Skip host if in Server 
                if (RequestedPlayType == PlayType.Server)
                {
                    continue;
                }

                if (player.Properties.TryGetValue("team", out PlayerProperty teamProp))
                {
                    if (teamProp.Value.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                    {
                        teamPlayers.Add(player);
                    }
                }
            }
            Debug.Log($"[PlayerHelpers] Team '{teamName}' has {teamPlayers.Count} players");
            return teamPlayers;
        }

        return null;
    }
    static public IReadOnlyPlayer FindCurrentPlayerForNetworkId(int networkId)
    {
        var sessionPlayers = ClientTransportHelper.instance.Session.Players;
        IReadOnlyPlayer currentPlayerId = sessionPlayers[networkId - 1];

        return currentPlayerId;
    }
}
