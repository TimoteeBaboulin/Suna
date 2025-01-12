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

    #region Properties
    public RoleType Role { get => _role; set => _role = value; }
    public World Server { get => _serverWorld; set => _serverWorld = value; }
    #endregion

    #region Messages
    private void Start()
    {
        if (Application.isEditor)
        {
            _role = RoleType.ServerClient;
        }
        else if (Application.platform == RuntimePlatform.WindowsServer)
        {
            _role = RoleType.Server;
        }
        else
        {
            _role = RoleType.Client;
        }

        Debug.Log($"Role : {_role}");
    }
    #endregion

    #region Public Methods
    public void Connect()
    {
        if (_role == RoleType.ServerClient || _role == RoleType.Client)
        {
            _clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
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