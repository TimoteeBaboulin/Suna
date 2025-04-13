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
        Debug.Log($"[AliveCheck] Team '{teamName}' session player count: {teamList.Count}");

        int aliveCount = 0;

        // ---------- QUERY A: Get all client entities with ClientComponent ----------
        EntityQuery clientQuery = world.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ClientComponent) }
        });
        NativeArray<Entity> clientEntities = clientQuery.ToEntityArray(Allocator.Temp);

        // Build a dictionary to look up the client’s playerID by its Entity.
        Dictionary<Entity, string> clientIdLookup = new Dictionary<Entity, string>();
        for (int i = 0; i < clientEntities.Length; i++)
        {
            Entity e = clientEntities[i];
            ClientComponent clientComp = world.EntityManager.GetComponentData<ClientComponent>(e);
            clientIdLookup[e] = clientComp.playerID.ToString();
            Debug.Log($"[AliveCheck] Client entity {e.Index} with playerID: {clientIdLookup[e]}");
        }
        clientEntities.Dispose();


        // ---------- QUERY B: Get character entities with CharacterClientAttachedComponent and CharacterIsEnable ----------
        EntityQueryDesc characterDesc = new EntityQueryDesc
        {
            All = new ComponentType[]
            {
            typeof(CharacterClientAttachedComponent),
            typeof(CharacterIsEnable)
            },
            Options = EntityQueryOptions.IgnoreComponentEnabledState
        };

        EntityQuery characterQuery = world.EntityManager.CreateEntityQuery(characterDesc);
        NativeArray<Entity> characterEntities = characterQuery.ToEntityArray(Allocator.Temp);
        Debug.Log($"[AliveCheck] Total character entities: {characterEntities.Length}");

        // Loop through character entities.
        for (int i = 0; i < characterEntities.Length; i++)
        {
            Entity characterEntity = characterEntities[i];
            var entityManager = world.EntityManager;

            // Check if the character's CharacterIsEnable is actually enabled.
            if (!entityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
                continue;

            // Get the attached client entity from the character.
            CharacterClientAttachedComponent attached = entityManager.GetComponentData<CharacterClientAttachedComponent>(characterEntity);
            Entity clientEntity = attached.ClientEntity;

            ClientComponent client = entityManager.GetComponentData<ClientComponent>(clientEntity);
            Debug.LogWarning($"[AliveCheck] client.playerID {client.playerID}");

            // Look up the client player ID from our dictionary.
            if (!clientIdLookup.TryGetValue(clientEntity, out string clientPlayerId))
            {
                Debug.LogWarning($"[AliveCheck] No ClientComponent found for client entity {clientEntity.Index}");
                continue;
            }

            // Compare clientPlayerId against the team session list.
            bool found = teamList.Exists(player =>
            {
                Debug.Log($"[AliveCheck] Comparing client ID '{clientPlayerId}' with session player ID '{player.Id}'");
                return player.Id == clientPlayerId;
            });

            if (found)
            {
                aliveCount++;
                Debug.Log($"[AliveCheck] Matched client playerID: {clientPlayerId} for character entity {characterEntity.Index}");
            }
        }
        characterEntities.Dispose();

        Debug.Log($"[AliveCheck] Final alive count for team '{teamName}': {aliveCount}");
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
