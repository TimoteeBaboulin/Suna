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
    }

    public struct TeamList
    {
        public List<IReadOnlyPlayer> natifPlayers;
        public List<IReadOnlyPlayer> corpoPlayers;
    }

    private static TeamList _teams = new TeamList
    {
        natifPlayers = new List<IReadOnlyPlayer>(),
        corpoPlayers = new List<IReadOnlyPlayer>()
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
            if (networkId > 1)
            {
                index = networkId;
            }
        }
        return (IPlayer)sessionPlayers[index];
    }

    static public string AssignTeamToPlayer(IReadOnlyPlayer readOnlyPlayer)
    {
        GlobalTeamCount teamCounts = GetCurrentTeamCounts();

        string assignedTeam = (teamCounts.corpoPlayersCount == 0 && teamCounts.natifPlayersCount == 0)
            ? (UnityEngine.Random.value < 0.5f ? "Corpo" : "Natif")
            : (teamCounts.corpoPlayersCount <= teamCounts.natifPlayersCount ? "Corpo" : "Natif");

        if (readOnlyPlayer is IPlayer player)
        {
            player.SetProperty("team", new PlayerProperty(assignedTeam, VisibilityPropertyOptions.Public));
        }

        if (assignedTeam == "Corpo")
        {
            _teams.corpoPlayers.Add(readOnlyPlayer);
            var session = ClientTransportHelper.instance.Session.AsHost();
            session.SetProperty("CountTeamCorpo", new SessionProperty(GetPlayersByTeam(TeamSideType.Corpo).Count.ToString()));
            session.SavePropertiesAsync();
        }
        else
        {
            _teams.natifPlayers.Add(readOnlyPlayer);
            var session = ClientTransportHelper.instance.Session.AsHost();
            session.SetProperty("CountTeamNatif", new SessionProperty(GetPlayersByTeam(TeamSideType.Natif).Count.ToString()));
            session.SavePropertiesAsync();
        }

        return assignedTeam;
    }
    public static void RemovePlayer(string playerId)
    {
        var corpo = _teams.corpoPlayers.FirstOrDefault(p => p.Id == playerId);
        if (corpo != null)
        {
            Debug.Log($"Player removed from CORPO ID {playerId} ");
            _teams.corpoPlayers.Remove(corpo);
            var session = ClientTransportHelper.instance.Session.AsHost();
            session.SetProperty("CountTeamCorpo", new SessionProperty(GetPlayersByTeam(TeamSideType.Corpo).Count.ToString()));
            session.SavePropertiesAsync();
            return;
        }

        var natif = _teams.natifPlayers.FirstOrDefault(p => p.Id == playerId);
        if (natif != null)
        {
            Debug.Log($"Player removed from NATIF ID {playerId} ");
            _teams.natifPlayers.Remove(natif);
            var session = ClientTransportHelper.instance.Session.AsHost();
            session.SetProperty("CountTeamNatif", new SessionProperty(GetPlayersByTeam(TeamSideType.Natif).Count.ToString()));
            session.SavePropertiesAsync();
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
            corpoPlayersCount = _teams.corpoPlayers.Count,
            natifPlayersCount = _teams.natifPlayers.Count
        };
    }

    public static TeamList GetTeamList()
    {
        return _teams;
    }

    public static IReadOnlyList<IReadOnlyPlayer> GetPlayersByTeam(TeamSideType teamSide)
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

    public static IReadOnlyList<IReadOnlyPlayer> GetPlayersByTeam(string teamName)
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

    public static int GetPlayersCountByTeamOnClient(string teamName)
    {
        TeamSideType teamSide = teamName == "Corpo" ? TeamSideType.Corpo : TeamSideType.Natif;
        switch (teamSide)
        {
            case TeamSideType.Corpo:
                return int.Parse(ClientTransportHelper.instance.Session.Properties["CountTeamCorpo"].Value);
            case TeamSideType.Natif:
                return int.Parse(ClientTransportHelper.instance.Session.Properties["CountTeamNatif"].Value);
            default:
                return 0;
        }
    }

    public static int GetPlayersCountByTeamOnClient(TeamSideType teamSide)
    {
        switch (teamSide)
        {
            case TeamSideType.Corpo:
                return int.Parse(ClientTransportHelper.instance.Session.Properties["CountTeamCorpo"].Value);
            case TeamSideType.Natif:
                return int.Parse(ClientTransportHelper.instance.Session.Properties["CountTeamNatif"].Value);
            default:
                return 0;
        }
    }

    public static void ClearTeam(string teamName)
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

    public static void ClearTeam(TeamSideType teamSide)
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
    }

    static public TeamSideType GetPlayerInTeam(int networkId)
    {
        var player = FindCurrentPlayerForNetworkId(networkId);

        if (player.Properties.Count > 0)
        {
            Debug.Log($"Player propeties count {player.Properties.Count}");
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
}
