using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport.Relay;
using Unity.Networking.Transport;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace GameNetwork
{
    struct DriverConfiguration
    {
        public NetworkRole Role;
        public NetworkType Type;
        public RelayServerData RelayClientData;
        public RelayServerData RelayServerData;
    }
    public class DriverConstructor : INetworkStreamDriverConstructor
    {
        private DriverConfiguration driverConfiguration;

        private static NetCodeConfig netcodeConfig;

        public DriverConstructor(NetworkConfiguration configuration)
        {
            driverConfiguration = new DriverConfiguration
            {
                Role = configuration.Role,
                Type = configuration.Type,
                RelayClientData = configuration.RelayClientData,
                RelayServerData = configuration.RelayServerData
            };
        }

        public DriverConstructor(NetworkRole role)
        {
            driverConfiguration = new DriverConfiguration
            {
                Role = role,
                Type = NetworkType.Direct,
            };
        }
        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var networkSettings = GameNetworkSettings(
                sendQueueCapacity: netcodeConfig.ClientSendQueueCapacity,
                receiveQueueCapacity: netcodeConfig.ClientReceiveQueueCapacity);
#if UNITY_EDITOR
            if (NetworkSimulatorSettings.Enabled)
            {
                NetworkSimulatorSettings.SetSimulatorSettings(ref networkSettings);
            }
#endif

            DefaultDriverBuilder.RegisterClientDriver(world, ref driverStore, netDebug, networkSettings);
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
#if UNITY_EDITOR 
            var networkSettings = GameNetworkSettings(sendQueueCapacity: netcodeConfig.ServerSendQueueCapacity, receiveQueueCapacity: netcodeConfig.ServerReceiveQueueCapacity);

            // Ipc driver is not needed unless we are self-connecting.
            if (driverConfiguration.Type == NetworkType.DistributedAuthority)
            {
                networkSettings.WithRelayParameters(ref driverConfiguration.RelayServerData);
            }
            DefaultDriverBuilder.RegisterServerUdpDriver(world, ref driverStore, netDebug, networkSettings);
#endif
        }

        static NetworkSettings GameNetworkSettings(int sendQueueCapacity, int receiveQueueCapacity)
        {
            var networkSettings = new NetworkSettings(Allocator.Temp);
            networkSettings.WithNetworkConfigParameters(
                connectTimeoutMS: netcodeConfig.ConnectTimeoutMS,
                maxConnectAttempts: netcodeConfig.MaxConnectAttempts,
                disconnectTimeoutMS: netcodeConfig.DisconnectTimeoutMS,
                reconnectionTimeoutMS: netcodeConfig.ReconnectionTimeoutMS,
                sendQueueCapacity: sendQueueCapacity,
                receiveQueueCapacity: receiveQueueCapacity
            );
            return networkSettings;
        }
    }
}
