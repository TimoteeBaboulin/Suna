using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Multiplayer.Widgets;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Multiplayer;
using Unity.Services.Vivox;
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


    public class ClientTransportHelper
    {
        //public ushort Port { get; private set; }
        //public string IP { get; private set; }
        //public bool IsClientLocal { get; private set; }
        //public bool AllowConnection { get; private set; } = true;

        public ISession Session { get; set; }
        public NetworkEndpoint ListenEndpoint { get; private set; }
        public NetworkEndpoint ConnectEndpoint { get; private set; }
        public NetworkType SessionConnectionType { get; private set; }

        public static string SessionID { get; set; }
        public static string CurrentIP { get; set; } /*= "51.210.222.138";*/
        public static ushort CurrentPort { get; set; } = 7979;
        public static bool isClientLocal { get; set; } = false;
        public static ClientConnectionState State = ClientConnectionState.NotConnected;
        public static int MaxNbOfPlayers = 11;
        public static World ClientWorld { get; set; } = null;
        public static World ServerWorld { get; set; } = null;

        public static ClientTransportHelper instance { get; set; }

        public async Task<ClientTransportHelper> CreateOrJoinSessionAsync(string sessionId, CancellationToken cancellationToken)
        {
            //await StartServicesAsync();
            instance = new ClientTransportHelper();
            // gameConnection.State = ClientConnectionState.Matchmaking;

            var options = instance.CreateSessionOptions();
            var networkHandler = new NetworkHandler();
            options.WithNetworkHandler(networkHandler);

            instance.Session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options);
            instance.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            instance.ListenEndpoint = await networkHandler.ListenEndpoint;
            instance.SessionConnectionType = await networkHandler.SessionConnectionType;

            return instance;
        }

        public async Task<ClientTransportHelper> CreateServerSessionAsync(SessionOptions sessionOptions)
        {
            try
            {
                //await StartServicesAsync();

                Debug.Log($"[SessionTransportHelper] Starting CreateServerSessionAsync with IP: {CurrentIP}, Port: {CurrentPort}");

                instance = new ClientTransportHelper();

                var options = instance.CreateSessionOptions();
                Debug.Log($"[SessionTransportHelper] Default session options created. Overriding MaxPlayers from {options.MaxPlayers} to {sessionOptions.MaxPlayers}");

                options.MaxPlayers = sessionOptions.MaxPlayers;
                options.Name = sessionOptions.Name;

                var networkHandler = new NetworkHandler();
                options.WithNetworkHandler(networkHandler);
                Debug.Log("[SessionTransportHelper] NetworkHandler attached to session options.");

                IHostSession hostSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                //IServerSession hostSession = await MultiplayerServerService.Instance.CreateSessionAsync(options);
                if (hostSession == null)
                {
                    Debug.LogError("[SessionTransportHelper] CreateSessionAsync returned null host session!");
                    return instance;
                }

                instance.Session = hostSession;
                Debug.Log($"[SessionTransportHelper] Server created session with official ID: {hostSession.Id}");
                Debug.Log($"[SessionTransportHelper] MaxPlayer Host: {hostSession.MaxPlayers}, Max Player session Options {sessionOptions.MaxPlayers}");

                instance.ConnectEndpoint = await networkHandler.ConnectEndpoint;
                instance.ListenEndpoint = await networkHandler.ListenEndpoint;
                instance.SessionConnectionType = await networkHandler.SessionConnectionType;
                Debug.Log($"[SessionTransportHelper] Endpoints retrieved: " +
                    $"ConnectEndpoint={instance.ConnectEndpoint}, " +
                    $"ListenEndpoint={instance.ListenEndpoint}, " +
                    $"SessionConnectionType={instance.SessionConnectionType}");

                return instance;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SessionTransportHelper] Error in CreateServerSessionAsync: {ex}");
                throw;
            }
        }
        public async Task<ClientTransportHelper> JoinSessionByIdAsync(string sessionID, CancellationToken cancellationToken)
        {
            instance = new ClientTransportHelper();
            await StartServicesAsync();
            //gameConnection.State = ClientConnectionState.Matchmaking;

            var joinOptions = new JoinSessionOptions();
            var networkHandler = new NetworkHandler();
            joinOptions.WithNetworkHandler(networkHandler);

            instance.Session = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionID, joinOptions);
            instance.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            instance.ListenEndpoint = await networkHandler.ListenEndpoint;
            instance.SessionConnectionType = await networkHandler.SessionConnectionType;

            Debug.Log($"Client joined session with code: {instance.Session.Id}");
            return instance;
        }

        public async Task<ClientTransportHelper> ReconnectByIdAsync(string sessionID, CancellationToken cancellationToken)
        {
            var gameConnection = new ClientTransportHelper();

            var reconnectOptions = new ReconnectSessionOptions();
            var networkHandler = new NetworkHandler();

            reconnectOptions.WithNetworkHandler(networkHandler);
            gameConnection.Session = await MultiplayerService.Instance.ReconnectToSessionAsync(sessionID);
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;

            Debug.Log($"Client reconnected to session with id: {gameConnection.Session.Id}");
            return gameConnection;
        }

        public async Task<ClientTransportHelper> JoinOrCreateMatchmakerGameAsync(CancellationToken cancellationToken)
        {
            var gameConnection = new ClientTransportHelper();
            //await StartServicesAsync();
            //gameConnection.State = ClientConnectionState.Matchmaking;

            var options = gameConnection.CreateSessionOptions();

            var networkHandler = new NetworkHandler();
            options.WithNetworkHandler(networkHandler);

            MatchmakerOptions match = new MatchmakerOptions
            {
                QueueName = "1vs0",
            };

            gameConnection.Session = await MultiplayerService.Instance.MatchmakeSessionAsync(match, options, cancellationToken);
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;

            return gameConnection;
        }

        public async Task<ClientTransportHelper> MatchmakeSessionAsync(MatchmakerOptions matchOptions,
            SessionOptions sessionOptions,
            CancellationToken token = default)
        {
            try
            {
                Debug.Log("[SessionTransportHelper] Starting matchmaking...");

                Session = await MultiplayerService.Instance.MatchmakeSessionAsync(matchOptions, sessionOptions, token);
                if (Session == null)
                {
                    Debug.LogError("[SessionTransportHelper] Matchmaking returned a null session.");
                    return null;
                }

                SessionID = Session.Id;
                Debug.Log($"[SessionTransportHelper] Matchmade session. Session ID: {Session.Id}, Code: {Session.Code}");

                return this;
            }
            catch (SessionException ex)
            {
                Debug.LogError($"[SessionTransportHelper] Error during matchmaking: {ex.Message}");
                return null;
            }
        }

        public async Task<ClientTransportHelper> QuickJoinSessionAsync(QuickJoinOptions quickJoinOptions,
            SessionOptions sessionOptions)
        {
            try
            {
                Debug.Log("[SessionTransportHelper] Starting quick join matchmaking...");
                Session = await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);

                if (Session == null)
                {
                    Debug.LogError("[SessionTransportHelper] Quick join returned a null session.");
                    return null;
                }

                SessionID = Session.Id;
                Debug.Log($"[SessionTransportHelper] Quick-joined session. Session ID: {Session.Id}, Code: {Session.Code}");


                return this;
            }
            catch (SessionException ex)
            {
                Debug.LogError($"[SessionTransportHelper] Error during quick join: {ex.Message}");
                return null;
            }
        }

        public static async Task StartServicesAsync()
        {
            if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized)
            {
                await Unity.Services.Core.UnityServices.InitializeAsync();
            }
            if (!Unity.Services.Authentication.AuthenticationService.Instance.IsAuthorized)
            {
                await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        public static async Task WaitForPlayerConnectionAsync(CancellationToken cancellationToken = default)
        {
            SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.WaitingConnection);
            State = ClientConnectionState.Connecting;
            while (State == ClientConnectionState.Connecting)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }
        public SessionOptions CreateSessionOptions()
        {
            int maxPlayers = ClientTransportHelper.MaxNbOfPlayers;
            var options = new SessionOptions
            {
                MaxPlayers = maxPlayers,
            };
            return options.WithDirectNetwork(CurrentIP, CurrentIP, CurrentPort);
        }
    }
}
