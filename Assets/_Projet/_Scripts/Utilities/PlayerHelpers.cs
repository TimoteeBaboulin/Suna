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
        Debug.Log($"[AliveCheck] Team '{teamName}' session player count: {teamList.Count}");

        int count = 0;

        EntityQueryDesc desc = new EntityQueryDesc
        {
            All = new ComponentType[]
            {
            typeof(CharacterClientAttachedComponent)
            },
        };

        EntityQuery query = world.EntityManager.CreateEntityQuery(desc);
        NativeArray<Entity> characters = query.ToEntityArray(Allocator.Temp);

        Debug.Log($"[AliveCheck] Total character entities: {characters.Length}");

        for (int i = 0; i < characters.Length; i++)
        {
            Entity characterEntity = characters[i];
            var entityManager = world.EntityManager;

            // Make sure CharacterIsEnable is enabled
            if (!entityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
                continue;

            // Get linked client entity from character
            if (!entityManager.HasComponent<CharacterClientAttachedComponent>(characterEntity))
            {
                Debug.LogWarning($"[AliveCheck] Character entity {characterEntity.Index} missing CharacterClientAttachedComponent.");
                continue;
            }

            var attached = entityManager.GetComponentData<CharacterClientAttachedComponent>(characterEntity);
            Entity clientEntity = attached.ClientEntity;

            if (!entityManager.HasComponent<ClientComponent>(clientEntity))
            {
                Debug.LogWarning($"[AliveCheck] Client entity {clientEntity.Index} missing ClientComponent.");
                continue;
            }

            var clientComp = entityManager.GetComponentData<ClientComponent>(clientEntity);
            string clientPlayerId = clientComp.playerID.ToString();
            Debug.Log($"[AliveCheck] ClientID '{clientPlayerId}'");
            bool found = teamList.Exists(player =>
            {
                Debug.Log($"[AliveCheck] Comparing client ID '{clientPlayerId}' with session player ID '{player.Id}'");
                return player.Id == clientPlayerId;
            });

            if (found)
            {
                Debug.Log($"[AliveCheck] ✅ Matched PlayerID: {clientPlayerId}");
                count++;
            }
        }

        characters.Dispose();

        Debug.Log($"[AliveCheck] --- Team {teamName} player IDs ---");
        foreach (IReadOnlyPlayer player in teamList)
        {
            Debug.Log($" - {player.Id}");
        }

        Debug.Log($"[AliveCheck] ✅ Final count of alive players in team '{teamName}': {count}");

        return count;
    }

    static public List<IReadOnlyPlayer> GetPlayersByTeam(string teamName)
    {
        var teamPlayers = new List<IReadOnlyPlayer>();
        var session = ServerSessionFactory.instance.Session;
        var players = session.Players;

        foreach (var player in players)
        {
            // Skip host if in Server or Host mode
            if ((RequestedPlayType == PlayType.Server) && player.Id == session.CurrentPlayer.Id)
                continue;

            if (player.Properties.TryGetValue("team", out PlayerProperty teamProp))
            {
                if (teamProp.Value.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    teamPlayers.Add(player);
                }
            }
        }

        return teamPlayers;
    }
}
