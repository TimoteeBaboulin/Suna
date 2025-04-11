using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using UnityEngine;

public static class PlayerHelpers
{
    /// <summary>
    /// Function used to receive the number of players alive in a team
    /// The function is managed so can't be used in ISystems or Burst Compiled methods
    /// Written by Timotee
    /// </summary>
    static public int CountPlayersAliveManaged(TeamSideType team, World world)
    {
	string teamName = "";
        if (team == TeamSideType.Corpo)
            teamName = "Corpo";
        else if (team == TeamSideType.Natif)
            teamName = "Natif";

        List<IReadOnlyPlayer> teamList = GetPlayersByTeam(teamName.ToString());

        Debug.Log($"Count {teamName.ToString()} : {teamList.Count}");

        ////       List<int> totalPlayerIDs = new List<int>();

        int count = 0;

        EntityQueryDesc desc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ClientComponent) }
        };
        EntityQuery query = world.EntityManager.CreateEntityQuery(desc);
        NativeArray<Entity> clients = query.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < clients.Length; i++)
        {
            Entity clientEntity = clients[i];

            var clientComponent = world.EntityManager.GetComponentData<ClientComponent>(clientEntity);
            var clientPlayerID = clientComponent.playerID.ToString();

            Debug.Log($"Retrieved clientPlayerID: {clientPlayerID}");

            bool foundPlayerId = teamList.Exists(player =>
            {
                Debug.Log($"Comparing team playerId: {player.Id} with clientPlayerID: {clientPlayerID}");
                return player.Id == clientPlayerID; 
            });

            if (!foundPlayerId)
                continue;

            Debug.Log($"Found player with playerID {clientPlayerID}");

            if (world.EntityManager.HasComponent<CharacterIsEnable>(clientEntity))
            {
                count++;
            }
        }

        foreach (IReadOnlyPlayer player in teamList)
        {
            Debug.Log(player.Id);
        }

        Debug.Log($"{count} players alive in {team.ToString()}");

        return count;
    }

    static public List<IReadOnlyPlayer> GetPlayersByTeam(string teamName)
    {
        var teamPlayers = new List<IReadOnlyPlayer>();
        var session = ServerSessionFactory.instance.Session;
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
        return teamPlayers;
    }
}
