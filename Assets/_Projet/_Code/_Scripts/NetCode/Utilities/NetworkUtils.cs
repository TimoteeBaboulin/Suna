using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.Widgets;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace GameNetwork.Utils
{
    public enum ClientConnectionState
    {
        NotConnected,
        Connecting,
        Connected,
        Matchmaking,
    }

    public class ClientConnection
	{
        // Now instance fields instead of static.
        public ClientConnectionState State { get; private set; } = ClientConnectionState.NotConnected;
        public ushort Port { get; private set; }
        public string IP { get; private set; }
        public bool IsClientLocal { get; private set; }
        public bool AllowConnection { get; private set; } = true;

        public ISession Session { get; private set; }
        public NetworkEndpoint ListenEndpoint;
        public NetworkEndpoint ConnectEndpoint;
        public NetworkType SessionConnectionType { get; private set; }

        // Constructor receives settings so that each instance can have its own configuration.
        public ClientConnection(string ip, ushort port, bool isClientLocal)
        {
            IP = ip;
            Port = port;
            IsClientLocal = isClientLocal;
        }

        public async Task<ClientConnection> JoinOrCreateMatchmakerGameAsync(CancellationToken cancellationToken)
        {
            // Create a new instance with the current settings.
            var gameConnection = new ClientConnection(IP, Port, IsClientLocal);

            await StartServicesAsync();

            gameConnection.State = ClientConnectionState.Matchmaking;
            var options = gameConnection.CreateSessionOptions();
            var networkHandler = new NetworkHandler();
            options.WithNetworkHandler(networkHandler);
            MatchmakerOptions match = new MatchmakerOptions
            {
                QueueName = "1vs1",
            };
            // Optionally update UI here, e.g., LoadingData.Instance.UpdateLoading(LoadingSteps.LookingForMatch);
            gameConnection.Session = await MultiplayerService.Instance.MatchmakeSessionAsync(match, options, cancellationToken);
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;

            return gameConnection;
        }

        private static async Task StartServicesAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsAuthorized)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        public SessionOptions CreateSessionOptions()
        {
            // Assume GameManager.Instance.MaxNbOfPlayer is available.
            SessionOptions options = new SessionOptions { MaxPlayers = GameManager.Instance.MaxNbOfPlayer };
            return options.WithDirectNetwork(IP, IP, Port);
        }
    }

}
