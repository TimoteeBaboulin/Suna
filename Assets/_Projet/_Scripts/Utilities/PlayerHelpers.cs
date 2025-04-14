using GameNetwork.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
        int aliveCount = 0;
        string teamName = team == TeamSideType.Corpo ? "Corpo" : "Natif";

        List<IReadOnlyPlayer> teamList = GetPlayersByTeam(teamName);
        if (teamList.Count == 0)
        {
            return 0;
        }

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

            if (!entityManager.HasComponent<CharacterIsEnable>(characterEntity) ||
                !entityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
            {
                continue; 
            }

            if (!entityManager.HasComponent<CharacterClientAttachedComponent>(characterEntity)){ continue; }

            CharacterClientAttachedComponent attached = entityManager.GetComponentData<CharacterClientAttachedComponent>(characterEntity);
            Entity clientEntity = attached.ClientEntity;

            if (!entityManager.HasComponent<ClientComponent>(clientEntity))
            {
                continue; 
            }

            ClientComponent client = entityManager.GetComponentData<ClientComponent>(clientEntity);
            bool found = false;

            // Compare client player ID with the team list
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
        //Debug.Log($"[AliveCheck] Total alive players for team '{teamName}': {aliveCount}");
        return aliveCount;
    }

    static public List<IReadOnlyPlayer> GetPlayersByTeam(string teamName)
    {
        var teamPlayers = new List<IReadOnlyPlayer>();
        if (ClientTransportHelper.instance != null && ClientTransportHelper.instance.Session != null)
        {
            var session = ClientTransportHelper.instance.Session;
            var players = session.Players;
            foreach (var player in players)
            {
                //if (RequestedPlayType == PlayType.Server)
                //{
                //    continue;
                //}

                if (player.Properties.TryGetValue("team", out PlayerProperty teamProp))
                {
                    if (teamProp.Value.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                    {
                        teamPlayers.Add(player);
                    }
                }
            }
        }
        return teamPlayers;
    }
    static public IReadOnlyPlayer FindCurrentPlayerForNetworkId(int networkId)
    {
        var sessionPlayers = ClientTransportHelper.instance.Session.Players;
        if (RequestedPlayType == PlayType.Server && networkId == 1)
        {
            networkId++;
        }

        IReadOnlyPlayer currentPlayerId = sessionPlayers[networkId - 1];
        return currentPlayerId;
    }

    static public string AssignTeamToPlayer(IReadOnlyPlayer readOnlyPlayer, IReadOnlyList<IReadOnlyPlayer> allPlayers)
    {
        int countTeamA = 0;
        int countTeamB = 0;

        foreach (var p in allPlayers)
        {
            if (p.Properties.TryGetValue("team", out PlayerProperty prop))
            {
                if (prop.Value == "Corpo")
                    countTeamA++;
                else if (prop.Value == "Natif")
                    countTeamB++;
            }
        }

        // Assign the team based on current counts, or randomly if both teams are empty
        string assignedTeam = (countTeamA == 0 && countTeamB == 0)
            ? (UnityEngine.Random.value < 0.5f ? "Corpo" : "Natif")
            : (countTeamA <= countTeamB ? "Corpo" : "Natif");

        // Set the team property for the player
        if (readOnlyPlayer is IPlayer player)
        {
            player.SetProperty("team", new PlayerProperty(assignedTeam, VisibilityPropertyOptions.Public));
            Debug.Log($"[Team Assignment] Assigned Player {player.AllocationId} to team {assignedTeam}");
        }

        return assignedTeam;
    }

    static public void UpdateTeamCountInSession(string assignedTeam, string playerId)
    {
        var session = ClientTransportHelper.instance.Session;
        if (session is IHostSession hostSession)
        {
            if (assignedTeam == "Corpo")
            {
                var countTeamCorpoProp = hostSession.Properties["CountTeamCorpo"];
                int currentCountCorpo = int.Parse(countTeamCorpoProp.Value);
                hostSession.SetProperty("CountTeamCorpo", new SessionProperty((currentCountCorpo + 1).ToString(), VisibilityPropertyOptions.Public));
               // hostSession.SavePropertiesAsync();
                hostSession.SavePlayerDataAsync(playerId);
            }
            else if (assignedTeam == "Natif")
            {
                var countTeamNatifProp = hostSession.Properties["CountTeamNatif"];
                int currentCountNatif = int.Parse(countTeamNatifProp.Value);
                hostSession.SetProperty("CountTeamNatif", new SessionProperty((currentCountNatif + 1).ToString(), VisibilityPropertyOptions.Public));
                //hostSession.SavePropertiesAsync();
                hostSession.SavePlayerDataAsync(playerId);
            }

            Debug.Log($"Updated Team Counts: Corpo = {hostSession.Properties["CountTeamCorpo"].Value}, Natif = {hostSession.Properties["CountTeamNatif"].Value}");
        }
    }

    static public void SubscribePlayerJoined(string playerId)
    {
        Debug.Log($"[Team Assignment] PlayerJoined triggered for ID: {playerId}");
        var session = ClientTransportHelper.instance.Session;
        foreach (var p in session.Players)
        {
            Debug.Log($"[Player Check] Session Player ID: {p.Id}");
        }

        var player = session.Players.FirstOrDefault(p => p.Id == playerId);

        if (player != null)
        {
            Debug.Log($"[Team Assignment] Match found! Assigning team to Player ID: {player.Id}");
            AssignTeamToPlayer(player, session.Players);
        }
        else
        {
            Debug.LogWarning($"[Team Assignment] No player found with ID: {playerId}");
        }
    }
}
