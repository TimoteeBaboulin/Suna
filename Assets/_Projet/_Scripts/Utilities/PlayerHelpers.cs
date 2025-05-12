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
        public List<IReadOnlyPlayer> natifPlayers;
        public List<IReadOnlyPlayer> corpoPlayers;
        public List<IReadOnlyPlayer> neutralPlayers;
    }

    private static TeamList _teams = new TeamList
    {
        natifPlayers = new List<IReadOnlyPlayer>(),
        corpoPlayers = new List<IReadOnlyPlayer>(),
        neutralPlayers = new List<IReadOnlyPlayer>()
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
        Debug.Log($"[AssignTeam] player check in find {sessionPlayers[index].Id}");
        return (IPlayer)sessionPlayers[index];
    }

    static public TeamSideType AssignTeamToPlayer(IReadOnlyPlayer readOnlyPlayer, TeamSideType team = TeamSideType.Neutre)
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



        switch (assignedTeam)
        {
            case TeamSideType.Corpo:
                if (_teams.corpoPlayers.Contains(readOnlyPlayer))
                    return TeamSideType.Neutre;

                _teams.corpoPlayers.Add(readOnlyPlayer);

                foreach (var item in _teams.neutralPlayers)
                {
                    Debug.Log($"[AssignTeam]add plyar :::: {item}");
                }
                Debug.Log($"[AssignTeam]add remove {readOnlyPlayer}");
                Debug.Log($"[AssignTeam]add remove {_teams.neutralPlayers.Contains(readOnlyPlayer)}");
                if (_teams.neutralPlayers.Contains(readOnlyPlayer))
                {
                    _teams.neutralPlayers.Remove(readOnlyPlayer);
                }
                else if (_teams.natifPlayers.Contains(readOnlyPlayer))
                {
                    _teams.natifPlayers.Remove(readOnlyPlayer);
                }
                return TeamSideType.Corpo;
            case TeamSideType.Natif:
                if (_teams.natifPlayers.Contains(readOnlyPlayer))
                    return TeamSideType.Neutre;

                _teams.natifPlayers.Add(readOnlyPlayer);
                if (_teams.corpoPlayers.Contains(readOnlyPlayer))
                {
                    _teams.corpoPlayers.Remove(readOnlyPlayer);
                }
                else if (_teams.neutralPlayers.Contains(readOnlyPlayer))
                {
                    _teams.neutralPlayers.Remove(readOnlyPlayer);
                }
                return TeamSideType.Natif;
            default:
                if (!_teams.neutralPlayers.Contains(readOnlyPlayer))
                {
                    _teams.neutralPlayers.Add(readOnlyPlayer);
                    Debug.Log($"[AssignTeam]add {_teams.neutralPlayers.Count}");
                }

                return TeamSideType.Neutre;
        }

        if (readOnlyPlayer is IPlayer player)
        {
            player.SetProperty("team", new PlayerProperty(assignedTeam.ToString(), VisibilityPropertyOptions.Public));
        }
    }
    public static void RemovePlayer(string playerId)
    {
        var corpo = _teams.corpoPlayers.FirstOrDefault(p => p.Id == playerId);
        if (corpo != null)
        {
            Debug.Log($"Player removed from CORPO ID {playerId} ");
            _teams.corpoPlayers.Remove(corpo);
        }

        var natif = _teams.natifPlayers.FirstOrDefault(p => p.Id == playerId);
        if (natif != null)
        {
            Debug.Log($"Player removed from NATIF ID {playerId} ");
            _teams.natifPlayers.Remove(natif);
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
            corpoPlayersCount = _teams.corpoPlayers.Count,
            natifPlayersCount = _teams.natifPlayers.Count,
            neutralPlayersCount = _teams.neutralPlayers.Count,
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



    public static IReadOnlyList<IReadOnlyPlayer> GetPlayersByTeamOnServer(TeamSideType teamSide)
    {
        switch (teamSide)
        {
            case TeamSideType.Corpo:
                return _teams.corpoPlayers;
            case TeamSideType.Natif:
                return _teams.natifPlayers;
            default:
                return Array.Empty<IReadOnlyPlayer>();
        }
    }

    public static IReadOnlyList<IReadOnlyPlayer> GetPlayersByTeamOnServer(string teamName)
    {
        TeamSideType teamSide = teamName == "Corpo" ? TeamSideType.Corpo : TeamSideType.Natif;
        switch (teamSide)
        {
            case TeamSideType.Corpo:
                return _teams.corpoPlayers;
            case TeamSideType.Natif:
                return _teams.natifPlayers;
            default:
                return Array.Empty<IReadOnlyPlayer>();
        }
    }

    public static void ClearTeamOnServer(string teamName)
    {
        TeamSideType teamSide = teamName == "Corpo" ? TeamSideType.Corpo : TeamSideType.Natif;
        switch (teamSide)
        {
            case TeamSideType.Corpo:
                _teams.corpoPlayers.Clear();
                break;
            case TeamSideType.Natif:
                _teams.natifPlayers.Clear();
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
                _teams.corpoPlayers.Clear();
                break;
            case TeamSideType.Natif:
                _teams.natifPlayers.Clear();
                break;
            default:
                Array.Empty<IReadOnlyPlayer>();
                break;
        }
    }

    public static void ClearTeams()
    {
        _teams.corpoPlayers.Clear();
        _teams.natifPlayers.Clear();
        _teams.neutralPlayers.Clear();
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
