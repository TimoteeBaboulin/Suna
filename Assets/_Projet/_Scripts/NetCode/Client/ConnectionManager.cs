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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Unity.Scenes.SceneSystem;

public class ConnectionManager : MonoBehaviour
{
    #region Fields
    [Header("Connection Settings")]
    [SerializeField] private bool clientLocal = false;
    [Tooltip("IP to reach/to connect on")]
    [SerializeField] private string _ip = "51.210.222.138";
    [SerializeField] private ushort _port = 7979;

    private string _localIp = "127.0.0.1";
    private ushort _localPort = 7979;

    private SubScene[] subScenes;
    public enum RoleType
    {
        ClientServer,
        Server,
        Client
    }

    private RoleType _role = RoleType.ClientServer;

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
            _role = RoleType.ClientServer;
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

        switch (_role)
        {
            case RoleType.ClientServer:
                _clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                _serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
                break;
            case RoleType.Server:
                _serverWorld = ClientServerBootstrap.ServerWorld;
                break;
            case RoleType.Client:
                _clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                break;
            default:
                Debug.LogError($"No world created client value{_clientWorld}, serverValue {_serverWorld}");
                break;
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

            string ip = default;
            ip = clientLocal ? _localIp :_ip;

            if (!clientLocal)
            {
                Debug.Log(ip);
            }
            ushort port = _port;

            if (_role == RoleType.ClientServer)
            {
                ip = _localIp;
                port = _localPort;
            }

            NetworkEndpoint connectionEndpoint = NetworkEndpoint.Parse(ip, port);
            {
                using EntityQuery networkDriverQuery = _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(_clientWorld.EntityManager, connectionEndpoint);
            }

            #if UNITY_EDITOR
            Debug.Log($"Started Client with roleType {_role}, IP : {ip}, port {port}");
            #endif
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

        foreach (var scene in subScenes)
        {
            scene.gameObject.SetActive(true);
        }
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
                var subSceneGUID = new Unity.Entities.Hash128(subScenes[i].SceneGUID.Value);
                SceneLoadFlags flag = SceneLoadFlags.BlockOnStreamIn;
#if UNITY_EDITOR
                flag = SceneLoadFlags.BlockOnImport;
#endif
                SceneSystem.LoadParameters loadParameters = new SceneSystem.LoadParameters() { Flags = flag };
                Entity sceneEntity = SceneSystem.LoadSceneAsync(world.Unmanaged, subSceneGUID, loadParameters);

                while (!SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
                {
                    world.Update();
                    yield return null; 
                }
            }
        }
    }
    #endregion
}