using GameNetwork.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Multiplayer.Playmode;
using Unity.NetCode;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using static System.Collections.Specialized.BitVector32;
using static Unity.NetCode.ClientServerBootstrap;

public static class PlayerHelpers
{
    public struct AliveCounts
    {
        public int natifPlayersAlive;
        public int corpoPlayersAlive;
    }

    public struct GlobalTeamCount
    {
        public int natifPlayersCount;
        public int corpoPlayersCount;
        public int neutralPlayersCount;
    }

    public struct TeamList
    {
        public List<string> natifPlayersId;
        public List<string> corpoPlayersid;
        public List<string> neutralPlayersId;
    }

    private static TeamList _teams = new TeamList
    {
        natifPlayersId = new List<string>(),
        corpoPlayersid = new List<string>(),
        neutralPlayersId = new List<string>()
    };

    private static int CountPlayersAliveForTeam(TeamSideType team, World world)
    {
        int aliveCount = 0;

        ComponentType teamTag;
        switch (team)
        {
            case TeamSideType.Corpo:
                teamTag = ComponentType.ReadOnly<CorpoTeamTag>();
                break;
            case TeamSideType.Natif:
                teamTag = ComponentType.ReadOnly<NatifTeamTag>();
                break;
            default:
                Debug.LogWarning($"Team {team} does not have a defined tag component.");
                return 0;
        }

        EntityManager entityManager = world.EntityManager;

        EntityQuery characterQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
            ComponentType.ReadOnly<CharacterClientAttachedComponent>(),
            ComponentType.ReadOnly<CharacterIsEnable>(),
            teamTag
            },
            Options = EntityQueryOptions.IgnoreComponentEnabledState // Allows checking enable state manually.
        });

        NativeArray<Entity> characterEntities = characterQuery.ToEntityArray(Allocator.Temp);
        //Debug.Log($"[CountPlayersAlive] Filtered entity count: {characterEntities.Length} for team {team}.");

        for (int i = 0; i < characterEntities.Length; i++)
        {
            Entity characterEntity = characterEntities[i];
            if (!entityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
            {
                continue;
            }
            aliveCount++;

            //Debug.Log($"[CountPlayersAlive] Counting entity {characterEntity} for team {team}.");
        }

        characterEntities.Dispose();
        return aliveCount;
    }

    static public IPlayer FindCurrentPlayerForNetworkId(int networkId)
    {
        var sessionPlayers = ClientTransportHelper.instance.Session.Players;
        int index = networkId - 1;

        if (RequestedPlayType == PlayType.Server)
        {
            index = networkId;
        }
        return (IPlayer)sessionPlayers[index];
    }

    static public TeamSideType AssignTeamToPlayer(IPlayer player, TeamSideType team = TeamSideType.Neutre)
    {
        GlobalTeamCount teamCounts = GetCurrentTeamCounts();

        int teamSize;

        switch (team)
        {
            case TeamSideType.Corpo:
                teamSize = teamCounts.corpoPlayersCount;
                break;
            case TeamSideType.Natif:
                teamSize = teamCounts.natifPlayersCount;
                break;
            case TeamSideType.Neutre:
                teamSize = 0;
                break;
            default:
                teamSize = 0;
                break;
        }

        TeamSideType assignedTeam = team;

        //TODO: Uncomment this is we want teams to be limited to half the player limit of the game
        //if (teamSize >= ClientTransportHelper.MaxNbOfPlayers / 2)
        //{
        //    Debug.Log($"Too many people in the team (Trying to spawn in team {team}, which have {teamSize} players and can only accept {(ClientTransportHelper.MaxNbOfPlayers - 1) / 2} players");
        //    return TeamSideType.Neutre;
        //}
        //else
        //{
        //    Debug.Log($"Spawning person in team {team}");
        //    assignedTeam = team;
        //}

        //int maxPlayers = ClientTransportHelper.MaxNbOfPlayers - 1;
        //int halfPoint = maxPlayers / 2;    

        //int totalConnected = teamCounts.corpoPlayersCount + teamCounts.natifPlayersCount;
        //TeamSideType assignedTeam;
        //if (totalConnected < halfPoint)
        //{
        //    assignedTeam = TeamSideType.Corpo;
        //}
        //else
        //{
        //    assignedTeam = TeamSideType.Natif;
        //}


        var corpo = _teams.corpoPlayersid.FirstOrDefault(id => id == player.Id);
        var natif = _teams.natifPlayersId.FirstOrDefault(id => id == player.Id);
        var neutral = _teams.neutralPlayersId.FirstOrDefault(id => id == player.Id);

        Debug.Log($"assignedTeam {assignedTeam}");
        switch (assignedTeam)
        {
            case TeamSideType.Corpo:
                if (corpo != null)
                {
                    Debug.Log($"[PlayerHelper] Already found player id {player.Id} in {_teams.corpoPlayersid}");
                    return TeamSideType.Neutre;
                }

                _teams.corpoPlayersid.Add(player.Id);
                if (neutral != null)
                {
                    _teams.neutralPlayersId.Remove(player.Id);
                    Debug.Log($"_teams.neutralPlayers.Count {_teams.neutralPlayersId.Count}");
                }
                else if (natif != null)
                {
                    _teams.natifPlayersId.Remove(player.Id);
                }

                player.SetProperty("team", new PlayerProperty(assignedTeam.ToString(), VisibilityPropertyOptions.Public));
                return TeamSideType.Corpo;
            case TeamSideType.Natif:
                if (natif != null)
                {
                    Debug.Log($"[PlayerHelper] Already found player id {player.Id} in {_teams.natifPlayersId}");
                    return TeamSideType.Neutre;
                }

                _teams.natifPlayersId.Add(player.Id);
                if (corpo != null)
                {
                    _teams.corpoPlayersid.Remove(player.Id);
                }
                else if (neutral != null)
                {
                    _teams.neutralPlayersId.Remove(player.Id);
                    Debug.Log($"_teams.neutralPlayers.Count {_teams.neutralPlayersId.Count}");
                }

                player.SetProperty("team", new PlayerProperty(assignedTeam.ToString(), VisibilityPropertyOptions.Public));
                return TeamSideType.Natif;
            default:
                if (neutral == null)
                    _teams.neutralPlayersId.Add(player.Id);

                player.SetProperty("team", new PlayerProperty(assignedTeam.ToString(), VisibilityPropertyOptions.Public));
                return TeamSideType.Neutre;
        }
    }
    public static void RemovePlayer(string playerId)
    {
        var corpo = _teams.corpoPlayersid.FirstOrDefault(id => id == playerId);
        if (corpo != null)
        {
            Debug.Log($"Player removed from CORPO ID {playerId} ");
            _teams.corpoPlayersid.Remove(corpo);
            return;
        }

        var natif = _teams.natifPlayersId.FirstOrDefault(id => id == playerId);
        if (natif != null)
        {
            Debug.Log($"Player removed from NATIF ID {playerId} ");
            _teams.natifPlayersId.Remove(natif);
            return;
        }

        var neutral = _teams.neutralPlayersId.FirstOrDefault(id => id == playerId);
        if (neutral != null)
        {
            Debug.Log($"Player removed from NEUTRAL ID {playerId} ");
            _teams.neutralPlayersId.Remove(natif);
            return;
        }
        Debug.LogWarning($"[PlayerHelpers] Tried to remove {playerId} but they weren’t in any team list");
    }


    private static int GetPlayersAlive(TeamSideType team, World world)
    {
        return CountPlayersAliveForTeam(team, world);
    }

    public static AliveCounts GetCurrentAliveCounts(World world)
    {
        AliveCounts counts;
        counts.natifPlayersAlive = GetPlayersAlive(TeamSideType.Natif, world);
        counts.corpoPlayersAlive = GetPlayersAlive(TeamSideType.Corpo, world);
        return counts;
    }


    public static GlobalTeamCount GetCurrentTeamCounts()
    {
        return new GlobalTeamCount
        {
            corpoPlayersCount = _teams.corpoPlayersid.Count,
            natifPlayersCount = _teams.natifPlayersId.Count,
            neutralPlayersCount = _teams.neutralPlayersId.Count,
        };
    }

    public static TeamList GetTeamList()
    {
        return _teams;
    }

    public static List<int> GetClientPlayerIdsByTeam(TeamSideType teamSide)
    {
        var world = ClientWorld;
        if (world == null || !world.IsCreated)
            return new List<int>();

        var em = world.EntityManager;

        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<GhostOwner>(),
            ComponentType.ReadOnly<ClientComponent>()
        );

        using (var owners = query.ToComponentDataArray<GhostOwner>(Allocator.TempJob))
        using (var clients = query.ToComponentDataArray<ClientComponent>(Allocator.TempJob))
        {
            var result = new List<int>(owners.Length);
            for (int i = 0; i < owners.Length; i++)
            {
                if (clients[i].team == teamSide)
                    result.Add(owners[i].NetworkId);
            }
            return result;
        }
    }

    public static List<ClientComponent> GetClientPlayersByTeam(TeamSideType teamSide)
    {
        var world = ClientWorld;
        if (world == null || !world.IsCreated)
            return new List<ClientComponent>();

        var em = world.EntityManager;
        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<GhostOwner>(),
            ComponentType.ReadOnly<ClientComponent>()
        );

        using (var clients = query.ToComponentDataArray<ClientComponent>(Allocator.TempJob))
        {
            var result = new List<ClientComponent>(clients.Length);
            for (int i = 0; i < clients.Length; i++)
            {
                if (clients[i].team == teamSide)
                    result.Add(clients[i]);
            }
            return result;
        }
    }



    public static IReadOnlyList<string> GetPlayersByTeamOnServer(TeamSideType teamSide)
    {
        switch (teamSide)
        {
            case TeamSideType.Corpo:
                return _teams.corpoPlayersid;
            case TeamSideType.Natif:
                return _teams.natifPlayersId;
            default:
                return Array.Empty<string>();
        }
    }

    public static IReadOnlyList<string> GetPlayersByTeamOnServer(string teamName)
    {
        TeamSideType teamSide = teamName == "Corpo" ? TeamSideType.Corpo : TeamSideType.Natif;
        switch (teamSide)
        {
            case TeamSideType.Corpo:
                return _teams.corpoPlayersid;
            case TeamSideType.Natif:
                return _teams.natifPlayersId;
            default:
                return Array.Empty<string>();
        }
    }

    public static void ClearTeamOnServer(string teamName)
    {
        TeamSideType teamSide = teamName == "Corpo" ? TeamSideType.Corpo : TeamSideType.Natif;
        switch (teamSide)
        {
            case TeamSideType.Corpo:
                _teams.corpoPlayersid.Clear();
                break;
            case TeamSideType.Natif:
                _teams.natifPlayersId.Clear();
                break;
            default:
                Array.Empty<IReadOnlyPlayer>();
                break;
        }
    }

    public static void ClearTeamOnServer(TeamSideType teamSide)
    {
        switch (teamSide)
        {
            case TeamSideType.Corpo:
                _teams.corpoPlayersid.Clear();
                break;
            case TeamSideType.Natif:
                _teams.natifPlayersId.Clear();
                break;
            default:
                Array.Empty<IReadOnlyPlayer>();
                break;
        }
    }

    public static void ClearTeams()
    {
        _teams.corpoPlayersid.Clear();
        _teams.natifPlayersId.Clear();
        _teams.neutralPlayersId.Clear();
    }

    static public TeamSideType GetPlayerInTeamOnServer(int networkId)
    {
        var player = FindCurrentPlayerForNetworkId(networkId);

        if (player.Properties.Count > 0)
        {
            string team = player.Properties["team"].Value;
            if (team == "Corpo")
            {
                return TeamSideType.Corpo;
            }
            else if (team == "Natif")
            {
                return TeamSideType.Natif;
            }
        }
        return TeamSideType.Neutre;
    }

    public static TeamSideType GetPlayerInTeam(int networkId)
    {
        var world = ClientWorld;
        if (world == null || !world.IsCreated)
            return TeamSideType.Neutre;

        var em = world.EntityManager;

        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<GhostOwner>(),
            ComponentType.ReadOnly<ClientComponent>()
        );

        using (var owners = query.ToComponentDataArray<GhostOwner>(Allocator.TempJob))
        using (var clients = query.ToComponentDataArray<ClientComponent>(Allocator.TempJob))
        {
            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i].NetworkId == networkId)
                    return clients[i].team;
            }
        }

        return TeamSideType.Neutre;
    }
}
