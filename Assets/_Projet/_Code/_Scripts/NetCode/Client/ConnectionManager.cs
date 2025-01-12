using System.Net;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionManager : Singleton<ConnectionManager>
{
    #region Fields
    [SerializeField] private string _ip = "127.0.0.1";
    [SerializeField] private ushort _port = 7979;

    public enum RoleType
    {
        ServerClient = 0,
        Server = 1,
        Client = 2
    }

    private RoleType _role = RoleType.ServerClient;

    private World _serverWorld = null;
    private World _clientWorld = null;
    #endregion


    #region Messages
    private void Start()
    {
        if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer)
        {
            _role = RoleType.ServerClient;
        }
        else if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
        {
            _role = RoleType.Server;
        }
        else if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Client)
        {
            _role = RoleType.Client;
        }

        Debug.Log($" Role local : {_role} \n Role Global {ClientServerBootstrap.RequestedPlayType}");
    }
    #endregion

    #region Public Methods
    public void Connect()
    {
        if (_clientWorld != null)
        {
            Debug.Log($"{_clientWorld} already created!");
            return;
        }

        if (_role == RoleType.ServerClient || _role == RoleType.Client)
        {
            _clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        }

        if (_role == RoleType.ServerClient)
        {
            _serverWorld = ClientServerBootstrap.ServerWorld;
        }

        DestroySimulationWorld();

        if (_serverWorld != null)
        {
            World.DefaultGameObjectInjectionWorld = _serverWorld;
        }
        else if (_clientWorld != null)
        {
            World.DefaultGameObjectInjectionWorld = _clientWorld;
            NetworkEndpoint connectionEndpoint = NetworkEndpoint.Parse(_ip, _port);
            {
                using EntityQuery networkDriverQuery = _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(_clientWorld.EntityManager, connectionEndpoint);
            }
        }
    }

    public void CreateServer()
    {
        _role = RoleType.Server;
        _serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
    }
    #endregion

    #region Private Methods
    private void DestroySimulationWorld()
    {
        foreach (var world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }
    }
    #endregion
}