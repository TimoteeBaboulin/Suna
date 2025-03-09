using System;
using System.Collections;
using System.Net;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionManager : Singleton<ConnectionManager>
{
    #region Fields
    [SerializeField] private string _ip = "141.94.194.103";
    [SerializeField] private ushort _port = 7979;

    private string _localIp = "127.0.0.1";
    private ushort _localPort = 7979;

    //public event Action Connected;
    SubScene[] subScenes;
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
    public World Server => _serverWorld;
    public World Client => _clientWorld;
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

        Debug.Log($" Role : {_role}");
    }
    #endregion

    #region Public Methods
    public void Connect()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (_clientWorld != null)
        {
            Debug.Log($"{_clientWorld} already created!");
            return;
        }

        if (_serverWorld != null)
        {
            Debug.Log($"{_serverWorld} already created!");
            return;
        }

        if (_role == RoleType.ServerClient || _role == RoleType.Client)
        {
            _clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        }

        if (_role == RoleType.ServerClient)
        {
            _serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        }

        DestroySimulationWorld();

        if (_serverWorld != null)
        {
            World.DefaultGameObjectInjectionWorld = _serverWorld;

            NetworkEndpoint serverEndPoint = NetworkEndpoint.AnyIpv4.WithPort(_localPort);
            {
                using EntityQuery networkDriverQuery = _serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndPoint);
            }
        }

        if (_clientWorld != null)
        {
            World.DefaultGameObjectInjectionWorld = _clientWorld;

            string ip = _ip;
            ushort port = _port;

            if (_role == RoleType.ServerClient)
            {
                ip = _localIp;
                port = _localPort;
            }

            NetworkEndpoint connectionEndpoint = NetworkEndpoint.Parse(ip, port);
            {
                using EntityQuery networkDriverQuery = _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(_clientWorld.EntityManager, connectionEndpoint);
            }
        }

        subScenes = FindObjectsByType<SubScene>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (_serverWorld != null)
        {
            StartCoroutine(LoadSubScenes(subScenes, _serverWorld));
        }

        if (_clientWorld != null)
        {
            StartCoroutine(LoadSubScenes(subScenes, _clientWorld));
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

    private IEnumerator LoadSubScenes(SubScene[] subScenes, World world)
    {
        while (!world.IsCreated)
        {
            yield return null;
        }
        if (subScenes != null)
        {
            for (int i = 0; i < subScenes.Length; i++)
            {
                SceneLoadFlags flag = SceneLoadFlags.BlockOnStreamIn;
                #if UNITY_EDITOR
                flag = SceneLoadFlags.BlockOnImport;
                #endif
                SceneSystem.LoadParameters loadParameters = new SceneSystem.LoadParameters() { Flags = flag };
                Entity sceneEntity = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(subScenes[i].SceneGUID.Value),
                    loadParameters);

                while (!SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
                {
                    world.Update();
                    yield return null; //Coucou ici, ça attends la fin de la frame pour confirmer et passer à la suivante, bisous :)
                }
            }
        }
    }
    #endregion
}