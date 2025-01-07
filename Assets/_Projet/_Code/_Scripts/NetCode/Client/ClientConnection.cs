using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public class ClientConnection : MonoBehaviour
{
    [SerializeField] private string _adresse = "127.0.0.1";
    [SerializeField] private ushort _port = 7979;

    private World _clientWorld;

#if UNITY_CLIENT || UNITY_EDITOR
    public void ConnectClient()
    {
        if (_clientWorld != null)
        {
            Debug.Log("Client is already created.");
            return;
        }

        DestroySimulationWorld();

        _clientWorld = ClientServerBootstrap.CreateClientWorld("Client");
        if (_clientWorld != null)
        {
            NetworkEndpoint connectionEndpoint = NetworkEndpoint.Parse(_adresse, _port);
            {
                using EntityQuery networkDriverQuery = _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(_clientWorld.EntityManager, connectionEndpoint);
            }
        }
        else
        {
            Debug.LogError("Failed to create client world.");
        }
    }
#endif

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
}