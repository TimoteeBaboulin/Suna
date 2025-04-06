using GameNetwork.Utils;
using System;
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

        private readonly TaskCompletionSource<NetworkEndpoint> connectEndpoint = new();
        private readonly TaskCompletionSource<NetworkEndpoint> listenEndpoint = new();
        private readonly TaskCompletionSource<NetworkType> sessionConnectionType = new();

        private NetCodeConfig netcodeConfig;

        public Task StartAsync(NetworkConfiguration configuration)
        {
            NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(configuration);

            var connectEp = GetConnectEndpoint(configuration);
            var listenEp = GetListenEndpoint(configuration);

            if (connectEp.Family == NetworkFamily.Invalid)
                throw new InvalidOperationException("ConnectEndpoint is invalid.");

            if (listenEp.Family == NetworkFamily.Invalid)
            {
                Debug.Log($"ListenEndpoint is invalid. Family: {listenEp.Family}, IP: {listenEp.Address}");
                throw new InvalidOperationException("ListenEndpoint is invalid.");
            }

            connectEndpoint.SetResult(connectEp);
            listenEndpoint.SetResult(listenEp);
            sessionConnectionType.SetResult(configuration.Type);

            return Task.CompletedTask;
        }

        private static NetworkEndpoint GetConnectEndpoint(NetworkConfiguration configuration)
        {
            //return (configuration.Type, configuration.Role) switch
            //{
            //    (NetworkType.Direct, NetworkRole.Host) =>
            //        NetworkEndpoint.LoopbackIpv4.WithPort(configuration.DirectNetworkListenAddress.Port),
            //    (NetworkType.Direct, NetworkRole.Client) =>
            //        configuration.DirectNetworkPublishAddress,
            //    (NetworkType.Relay, NetworkRole.Host) =>
            //        NetworkEndpoint.LoopbackIpv4.WithPort(configuration.RelayServerData.Endpoint.Port),
            //    (NetworkType.Relay, NetworkRole.Client) =>
            //        configuration.RelayClientData.Endpoint,
            //    _ => throw new InvalidOperationException(
            //        $"Invalid NetworkType/Role combination: {configuration.Type}, {configuration.Role}")
            //};

            switch (configuration.Type, configuration.Role)
            {
                case (NetworkType.Direct, NetworkRole.Host):
                    return NetworkEndpoint.LoopbackIpv4.WithPort(configuration.DirectNetworkListenAddress.Port);
                case (NetworkType.Direct, NetworkRole.Client):
                    return configuration.DirectNetworkPublishAddress;
                case (NetworkType.Relay, NetworkRole.Host):
                    return NetworkEndpoint.LoopbackIpv4.WithPort(configuration.RelayServerData.Endpoint.Port);
                case (NetworkType.Relay, NetworkRole.Client):
                    return configuration.RelayClientData.Endpoint;
                default:
                    return default;
            }
        }

        private static NetworkEndpoint GetListenEndpoint(NetworkConfiguration configuration)
        {
            return (configuration.Type, configuration.Role) switch
            {
                (NetworkType.Direct, NetworkRole.Host) => configuration.DirectNetworkListenAddress,
                (NetworkType.Direct, NetworkRole.Client) => NetworkEndpoint.LoopbackIpv4.WithPort(ClientTransportHelper.CurrentPort),
                (NetworkType.Relay, _) => NetworkEndpoint.AnyIpv4,
                _ => throw new InvalidOperationException($"Invalid configuration for listening: {configuration.Type}, {configuration.Role}")
            };
        }

        public Task StopAsync() => Task.CompletedTask;
        //public Task<NetworkEndpoint> ConnectEndpoint => connectEndpoint.Task;
        //public Task<NetworkEndpoint> ListenEndpoint => listenEndpoint.Task;
        //public Task<NetworkType> SessionConnectionType => sessionConnectionType.Task;

        //readonly TaskCompletionSource<NetworkEndpoint> connectEndpoint = new();
        //readonly TaskCompletionSource<NetworkEndpoint> listenEndpoint = new();
        //readonly TaskCompletionSource<NetworkType> sessionConnectionType = new();

        //NetCodeConfig netcodeConfig;
        //public Task StartAsync(NetworkConfiguration configuration)
        //{
        //    NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(configuration);
        //    connectEndpoint.SetResult(GetConnectEndpoint(configuration));
        //    listenEndpoint.SetResult(GetListenEndpoint(configuration));
        //    sessionConnectionType.SetResult(configuration.Type);
        //    return Task.CompletedTask;
        //}

        //static NetworkEndpoint GetConnectEndpoint(NetworkConfiguration configuration)
        //{
        //    switch (configuration.Type, configuration.Role)
        //    {
        //        case (NetworkType.Direct, NetworkRole.Host):
        //            return NetworkEndpoint.LoopbackIpv4.WithPort(configuration.DirectNetworkListenAddress.Port);
        //        case (NetworkType.Direct, NetworkRole.Client):
        //            return configuration.DirectNetworkPublishAddress;
        //        case (NetworkType.Relay, NetworkRole.Host):
        //            return NetworkEndpoint.LoopbackIpv4.WithPort(configuration.RelayServerData.Endpoint.Port);
        //        case (NetworkType.Relay, NetworkRole.Client):
        //            return configuration.RelayClientData.Endpoint;
        //        default:
        //            return default;
        //    }
        //}

        //static NetworkEndpoint GetListenEndpoint(NetworkConfiguration configuration)
        //{
        //    switch (configuration.Type)
        //    {
        //        case NetworkType.Direct:
        //            return configuration.DirectNetworkListenAddress;
        //        case NetworkType.Relay:
        //            return NetworkEndpoint.AnyIpv4;
        //        default:
        //            return default;
        //    }
        //}

        //public Task StopAsync()
        //{
        //    return Task.CompletedTask;
        //}
    }
}