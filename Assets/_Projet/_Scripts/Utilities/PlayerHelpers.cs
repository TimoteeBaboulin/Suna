using GameNetwork.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Multiplayer.Playmode;
using Unity.NetCode;
using Unity.Services.Matchmaker.Models;
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

            if (!entityManager.HasComponent<CharacterClientAttachedComponent>(characterEntity)) { continue; }

            CharacterClientAttachedComponent attached = entityManager.GetComponentData<CharacterClientAttachedComponent>(characterEntity);
            Entity clientEntity = attached.ClientEntity;

            if (!entityManager.HasComponent<ClientComponent>(clientEntity))
            {
                continue;
            }

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
        if (ClientTransportHelper.instance != null && ClientTransportHelper.instance.Session != null)
        {
            var session = ClientTransportHelper.instance.Session;
            var players = session.Players;
            foreach (var player in players)
            {
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
        int index = networkId - 1;

        if (RequestedPlayType == PlayType.Server && networkId == 1)
        {
            index = networkId; 
        }

        if (index < 0 || index >= sessionPlayers.Count)
        {
            Debug.LogError($"FindCurrentPlayerForNetworkId: index {index} hors limites (sessionPlayers.Count = {sessionPlayers.Count}) pour networkId {networkId}.");
            return null;
        }

        return sessionPlayers[index];
    }

    static public string AssignTeamToPlayer(IReadOnlyPlayer readOnlyPlayer, IReadOnlyList<IReadOnlyPlayer> allPlayers)
    {
        int countTeamCorpo = 0;
        int countTeamNatif = 0;

        foreach (var p in allPlayers)
        {
            if (p.Properties.TryGetValue("team", out PlayerProperty prop))
            {
                if (prop.Value == "Corpo")
                    countTeamCorpo++;
                else if (prop.Value == "Natif")
                    countTeamNatif++;
            }
        }

        string assignedTeam = (countTeamCorpo == 0 && countTeamNatif == 0)
            ? (UnityEngine.Random.value < 0.5f ? "Corpo" : "Natif")
            : (countTeamCorpo <= countTeamNatif ? "Corpo" : "Natif");

        if (readOnlyPlayer is IPlayer player)
        {
            player.SetProperty("team", new PlayerProperty(assignedTeam, VisibilityPropertyOptions.Public));

            var session = ClientTransportHelper.instance.Session;
            if (session is IHostSession hostSession)
                hostSession.SaveCurrentPlayerDataAsync();
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
            }
            else if (assignedTeam == "Natif")
            {
                var countTeamNatifProp = hostSession.Properties["CountTeamNatif"];
                int currentCountNatif = int.Parse(countTeamNatifProp.Value);
                hostSession.SetProperty("CountTeamNatif", new SessionProperty((currentCountNatif + 1).ToString(), VisibilityPropertyOptions.Public));
            }
            hostSession.SavePropertiesAsync();         
            Debug.Log($"[Final Save] Updated Team Counts: Corpo = {hostSession.Properties["CountTeamCorpo"].Value}, Natif = {hostSession.Properties["CountTeamNatif"].Value}");
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
            string team = AssignTeamToPlayer(player, session.Players);
            Debug.Log($"[Team Assignment] Match found! Assigning {team} to Player ID: {player.Id}");
        }
        else
        {
            Debug.LogWarning($"[Team Assignment] No player found with ID: {playerId}");
        }
    }
}
