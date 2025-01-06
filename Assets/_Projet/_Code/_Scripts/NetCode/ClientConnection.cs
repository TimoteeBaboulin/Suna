using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public class ClientConnection : MonoBehaviour
{
    [SerializeField] private string _adresse = "127.0.0.1";
    [SerializeField] private ushort _port = 7979;

#if UNITY_CLIENT || UNITY_EDITOR
    public void ConnectClient()
    {
        DestroySimulationWorld();

        World clientWorld = ClientServerBootstrap.CreateClientWorld("Client");

        NetworkEndpoint connectionEndpoint = NetworkEndpoint.Parse(_adresse, _port);
        {
            using EntityQuery networkDriverQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, connectionEndpoint);
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
