using System.Threading.Tasks;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace GameNetwork
{
    public class NetworkHandler : INetworkHandler
    {
        public Task<NetworkEndpoint> ConnectEndpoint => connectEndpoint.Task;
        public Task<NetworkEndpoint> ListenEndpoint => listenEndpoint.Task;
        public Task<NetworkType> SessionConnectionType => sessionConnectionType.Task;

        readonly TaskCompletionSource<NetworkEndpoint> connectEndpoint = new();
        readonly TaskCompletionSource<NetworkEndpoint> listenEndpoint = new();
        readonly TaskCompletionSource<NetworkType> sessionConnectionType = new();

        NetCodeConfig netcodeConfig;
        public Task StartAsync(NetworkConfiguration configuration)
        {
            NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(configuration);
            connectEndpoint.SetResult(GetConnectEndpoint(configuration));
            listenEndpoint.SetResult(GetListenEndpoint(configuration));
            sessionConnectionType.SetResult(configuration.Type);
            return Task.CompletedTask;
        }

        static NetworkEndpoint GetConnectEndpoint(NetworkConfiguration configuration)
        {
            switch (configuration.Type, configuration.Role)
            {
                case (NetworkType.Direct, NetworkRole.Host):
                    return NetworkEndpoint.LoopbackIpv4.WithPort(configuration.DirectNetworkListenAddress.Port);
                case (NetworkType.Direct, NetworkRole.Client):
                    return configuration.DirectNetworkPublishAddress;
                case (NetworkType.Relay, NetworkRole.Host):
                    return NetworkEndpoint.LoopbackIpv4.WithPort(configuration.RelayServerData.Endpoint.Port);
                default:
                    return default;
            }
        }

        static NetworkEndpoint GetListenEndpoint(NetworkConfiguration configuration)
        {
            switch (configuration.Type)
            {
                case NetworkType.Direct:
                    return configuration.DirectNetworkListenAddress;
                case NetworkType.Relay:
                    return NetworkEndpoint.AnyIpv4;
                default:
                    return default;
            }
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }
}