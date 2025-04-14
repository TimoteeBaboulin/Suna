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
        // Determine the team name.
        string teamName = team == TeamSideType.Corpo ? "Corpo" : "Natif";

        // Get the team list from session.
        List<IReadOnlyPlayer> teamList = GetPlayersByTeam(teamName);

        int aliveCount = 0;

        // Query for character entities with CharacterClientAttachedComponent and CharacterIsEnable
        EntityQuery characterQuery = world.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(CharacterClientAttachedComponent), typeof(CharacterIsEnable) },
            Options = EntityQueryOptions.IgnoreComponentEnabledState
        });

        NativeArray<Entity> characterEntities = characterQuery.ToEntityArray(Allocator.Temp);
        Debug.Log($"[AliveCheck] Total character entities: {characterEntities.Length}");
        Debug.Log($"[AliveCheck] Total character teamListCount: {teamList.Count}");

        // Loop through all character entities
        for (int i = 0; i < characterEntities.Length; i++)
        {
            Entity characterEntity = characterEntities[i];
            var entityManager = world.EntityManager;

            // Skip if the CharacterIsEnable component is not enabled
            if (!entityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
                continue;

            // Get the attached client entity from the character
            CharacterClientAttachedComponent attached = entityManager.GetComponentData<CharacterClientAttachedComponent>(characterEntity);
            Entity clientEntity = attached.ClientEntity;

            // Get the ClientComponent for the client associated with this character
            ClientComponent client = entityManager.GetComponentData<ClientComponent>(clientEntity);

            Debug.Log($"[AliveCheck] Checking client ID '{client.playerID}' against team session player IDs...");

            bool found = false;
            // Loop through the team list manually to compare player IDs
            for (int j = 0; j < teamList.Count; j++)
            {
                var player = teamList[j];

                Debug.Log($"[AliveCheck] Comparing client ID '{client.playerID}' with session player ID '{player.Id}'");

                // If IDs match, we found the correct player
                if (player.Id == client.playerID.ToString())
                {
                    found = true;
                    break; // Stop the loop once the correct match is found
                }
                Debug.Log($"[AliveCheck] Mismatched client ID '{client.playerID}' with session player ID '{player.Id}', skipping.");
            }

            // If a valid player match is found, increment alive count
            if (found)
            {
                aliveCount++;
                Debug.Log($"[AliveCheck] Found matching client playerID: {client.playerID} ClientID {client.playerID} " +
                    $"for character entity {characterEntity.Index}");
            }
            else
            {
                Debug.Log($"[AliveCheck] No match for client ID '{client.playerID}' in the team '{teamName}'");
            }
            Debug.Log($"[AliveCheck] found {found}");
        }
        characterEntities.Dispose();
        return aliveCount;
    }

    static public List<IReadOnlyPlayer> GetPlayersByTeam(string teamName)
    {
        var teamPlayers = new List<IReadOnlyPlayer>();
        var session = ClientTransportHelper.instance.Session;
        var players = session.Players;

        foreach (var player in players)
        {
            // Skip host if in Server or Host mode
            if ((RequestedPlayType == PlayType.Server) && player.Id == session.CurrentPlayer.Id)
            {
                Debug.Log($"[AliveCheck] skipped player '{player.Id}':currentSessionID {session.CurrentPlayer.Id} is host");
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

        return teamPlayers;
    }
    static public FixedString64Bytes FindCurrentPlayerIdForNetworkId(int networkId)
    {
        var sessionPlayers = ClientTransportHelper.instance.Session.Players;
        FixedString64Bytes currentPlayerId = sessionPlayers[networkId - 1].Id;

        return currentPlayerId;
    }

}
