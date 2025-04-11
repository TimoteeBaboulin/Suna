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
		teamName = "corpo";
	else if (team == TeamSideType.Natif)
		teamName = "natif";
        List<IReadOnlyPlayer> players = GameManager.Instance.GetPlayersByTeam(teamName);
        List<int> totalPlayerIDs = new List<int>();

        int count = 0;

        EntityQueryDesc desc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ClientComponent), typeof(GhostInstance) }
        };
        EntityQuery query = world.EntityManager.CreateEntityQuery(desc);

        NativeArray<Entity> clients = query.ToEntityArray(Allocator.Temp);
        NativeArray<GhostInstance> ghosts = query.ToComponentDataArray<GhostInstance>(Allocator.Temp);

        for (int i = 0; i < clients.Length; i++)
        {
            bool foundPlayerId = players.Exists(
                (obj) =>
                {
                    return int.Parse(obj.Id) == ghosts[i].ghostId;
                });

            if (!foundPlayerId)
                continue;

            //Debug.Log($"Found player with id {ghosts[i].ghostId}");

            if (world.EntityManager.HasComponent<CharacterIsEnable>(clients[i]))
            {
                count++;
            }
        }

        foreach (IReadOnlyPlayer player in players)
        {
            Debug.Log(player.Id);
        }

        //Debug.Log($"{count} players alive in {team.ToString()}");

        return count;
    }
}
