using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Multiplayer.Widgets;
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
        public ushort Port { get; private set; }
        public string IP { get; private set; }
        public bool IsClientLocal { get; private set; }
        public bool AllowConnection { get; private set; } = true;

        public ISession Session { get; private set; }
        public NetworkEndpoint ListenEndpoint { get; private set; }
        public NetworkEndpoint ConnectEndpoint { get; private set; }
        public NetworkType SessionConnectionType { get; private set; }

        public static string SessionID { get; set; }
        public static World ServerWorld { get; set; }
        public static ClientConnectionState State = ClientConnectionState.NotConnected;

        public ClientTransportHelper(string ip, ushort port, bool isClientLocal)
        {
            IP = ip;
            Port = port;
            IsClientLocal = isClientLocal;
        }


        /// <summary>
        /// Uses Unity's Multiplayer Service to either join or create a session using a ticket.
        /// </summary>
        public async Task<ClientTransportHelper> CreateOrJoinSessionAsync(string sessionId, CancellationToken cancellationToken)
        {
            //await StartServicesAsync();
            var gameConnection = new ClientTransportHelper(IP, Port, IsClientLocal);
           // gameConnection.State = ClientConnectionState.Matchmaking;

            var options = gameConnection.CreateSessionOptions();
            var networkHandler = new NetworkHandler();
            options.WithNetworkHandler(networkHandler);

            gameConnection.Session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options); //TEMP HERE DONT FORGET MOTHERFUCKER
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

                UnityEngine.Debug.Log($"[SessionTransportHelper] Starting CreateServerSessionAsync with IP: {IP}, Port: {Port}, IsClientLocal: {IsClientLocal}");

                // Create a new instance of SessionTransportHelper with the current settings.
                var connection = new ClientTransportHelper(IP, Port, IsClientLocal);

                // Build default session options using the instance's helper.
                var options = connection.CreateSessionOptions();
                UnityEngine.Debug.Log($"[SessionTransportHelper] Default session options created. Overriding MaxPlayers from {options.MaxPlayers} to {sessionOptions.MaxPlayers}");

                // Override MaxPlayers with the provided value.
                options.MaxPlayers = sessionOptions.MaxPlayers;

                // Attach a network handler
                var networkHandler = new NetworkHandler();
                options.WithNetworkHandler(networkHandler);
                UnityEngine.Debug.Log("[SessionTransportHelper] NetworkHandler attached to session options.");

                //options.SessionProperties = new Dictionary<string, SessionProperty>();

                //string customCode = GenerateRandomSessionCode(6);
                //options.SessionProperties["CustomSessionCode"] = new SessionProperty(customCode);
                //UnityEngine.Debug.Log($"[SessionTransportHelper] Assigned custom session code: {customCode}");
                // -----------------------------------------

                IHostSession hostSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                if (hostSession == null)
                {
                    UnityEngine.Debug.LogError("[SessionTransportHelper] CreateSessionAsync returned null host session!");
                    return connection;
                }

                connection.Session = hostSession;
                UnityEngine.Debug.Log($"[SessionTransportHelper] Server created session with official ID: {hostSession.Id}");
                UnityEngine.Debug.Log($"[SessionTransportHelper] MaxPlayer Host: {hostSession.MaxPlayers}, Max Player session Options {sessionOptions.MaxPlayers}");

                // Retrieve connection endpoints.
                connection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
                connection.ListenEndpoint = await networkHandler.ListenEndpoint;
                connection.SessionConnectionType = await networkHandler.SessionConnectionType;
                UnityEngine.Debug.Log($"[SessionTransportHelper] Endpoints retrieved: " +
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
        public async Task<ClientTransportHelper> JoinSessionByIdAsync(string sessionCode, CancellationToken cancellationToken)
        {
            var gameConnection = new ClientTransportHelper(IP, Port, IsClientLocal);
            //await StartServicesAsync();
            //gameConnection.State = ClientConnectionState.Matchmaking;

            var joinOptions = new JoinSessionOptions();
            var networkHandler = new NetworkHandler();
            joinOptions.WithNetworkHandler(networkHandler);

            gameConnection.Session = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionCode, joinOptions);
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;

            UnityEngine.Debug.Log($"Client joined session with code: {gameConnection.Session.Id}");
            return gameConnection;
        }

        public async Task<ClientTransportHelper> JoinOrCreateMatchmakerGameAsync(CancellationToken cancellationToken)
        {
            var gameConnection = new ClientTransportHelper(IP, Port, IsClientLocal);
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
            SessionOptions options = new SessionOptions { MaxPlayers = 3 }; //CAREFUL HERE MOTHERFUCKER
            return options.WithDirectNetwork(IP, IP, Port);
        }
    }
}
