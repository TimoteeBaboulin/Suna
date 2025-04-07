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

        public ISession Session { get; private set; }
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

        public async Task<ClientTransportHelper> CreateOrJoinSessionAsync(string sessionId, CancellationToken cancellationToken)
        {
            //await StartServicesAsync();
            var gameConnection = new ClientTransportHelper();
            // gameConnection.State = ClientConnectionState.Matchmaking;

            var options = gameConnection.CreateSessionOptions();
            var networkHandler = new NetworkHandler();
            options.WithNetworkHandler(networkHandler);

            gameConnection.Session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options);
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;

            return gameConnection;
        }

        public async Task<ClientTransportHelper> CreateServerSessionAsync(SessionOptions sessionOptions)
        {
            try
            {
                //await StartServicesAsync();

                Debug.Log($"[SessionTransportHelper] Starting CreateServerSessionAsync with IP: {CurrentIP}, Port: {CurrentPort}");

                var connection = new ClientTransportHelper();

                var options = connection.CreateSessionOptions();
                Debug.Log($"[SessionTransportHelper] Default session options created. Overriding MaxPlayers from {options.MaxPlayers} to {sessionOptions.MaxPlayers}");

                options.MaxPlayers = sessionOptions.MaxPlayers;
                options.Name = sessionOptions.Name;

                var networkHandler = new NetworkHandler();
                options.WithNetworkHandler(networkHandler);
                Debug.Log("[SessionTransportHelper] NetworkHandler attached to session options.");

                IHostSession hostSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                if (hostSession == null)
                {
                    Debug.LogError("[SessionTransportHelper] CreateSessionAsync returned null host session!");
                    return connection;
                }

                connection.Session = hostSession;
                Debug.Log($"[SessionTransportHelper] Server created session with official ID: {hostSession.Id}");
                Debug.Log($"[SessionTransportHelper] MaxPlayer Host: {hostSession.MaxPlayers}, Max Player session Options {sessionOptions.MaxPlayers}");

                connection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
                connection.ListenEndpoint = await networkHandler.ListenEndpoint;
                connection.SessionConnectionType = await networkHandler.SessionConnectionType;
                Debug.Log($"[SessionTransportHelper] Endpoints retrieved: " +
                    $"ConnectEndpoint={connection.ConnectEndpoint}, " +
                    $"ListenEndpoint={connection.ListenEndpoint}, " +
                    $"SessionConnectionType={connection.SessionConnectionType}");

                return connection;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SessionTransportHelper] Error in CreateServerSessionAsync: {ex}");
                throw;
            }
        }
        public async Task<ClientTransportHelper> JoinSessionByIdAsync(string sessionID, CancellationToken cancellationToken)
        {
            var gameConnection = new ClientTransportHelper();
            await StartServicesAsync();
            //gameConnection.State = ClientConnectionState.Matchmaking;

            var joinOptions = new JoinSessionOptions();
            var networkHandler = new NetworkHandler();
            joinOptions.WithNetworkHandler(networkHandler);

            gameConnection.Session = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionID, joinOptions);
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;

            Debug.Log($"Client joined session with code: {gameConnection.Session.Id}");
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
            var options = new SessionOptions { MaxPlayers = maxPlayers };

            int playersPerTeam = (maxPlayers - 1) / 2;

            for (int i = 0; i < maxPlayers - 1; i++)
            {
                string team = i < playersPerTeam ? "corpo" : "natif";
                options.PlayerProperties.Add(
                    $"player{i + 1}",
                    new PlayerProperty(team, VisibilityPropertyOptions.Public)
                );
            }

            Debug.Log($"Create sessions with IP {CurrentIP}, Port {CurrentPort}");

            //if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer)
            //{
            //    return options.WithDirectNetwork("0.0.0.0", CurrentIP, CurrentPort);
            //}
            //else
            //{
            //}
                return options.WithDirectNetwork(CurrentIP, CurrentIP, CurrentPort);
        }
    }
}
