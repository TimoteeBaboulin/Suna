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

namespace GameNetwork.Utils
{
    public enum ClientConnectionState
    {
        NotConnected,
        Connecting,
        Connected,
        Matchmaking,
    }


    public class SessionTransportHelper
    {
        // Instance fields for per-connection configuration.
        public ClientConnectionState State { get; private set; } = ClientConnectionState.NotConnected;
        public ushort Port { get; private set; }
        public string IP { get; private set; }
        public bool IsClientLocal { get; private set; }
        public bool AllowConnection { get; private set; } = true;

        public ISession Session { get; private set; }
        public NetworkEndpoint ListenEndpoint;
        public NetworkEndpoint ConnectEndpoint;
        public NetworkType SessionConnectionType { get; private set; }

        // A global static default port for convenience.
        public static string SessionID { get; set; }
        public static World ServerWorld { get; set; }

        // Constructor receives settings so that each instance can have its own configuration.
        public SessionTransportHelper(string ip, ushort port, bool isClientLocal)
        {
            IP = ip;
            Port = port;
            IsClientLocal = isClientLocal;
        }


        /// <summary>
        /// Uses Unity's Multiplayer Service to either join or create a session using a ticket.
        /// </summary>
        public async Task<SessionTransportHelper> CreateOrJoinSessionAsync(string sessionId, CancellationToken cancellationToken)
        {
            await StartServicesAsync();
            // Create a new instance with current settings.
            var gameConnection = new SessionTransportHelper(IP, Port, IsClientLocal);
            //await StartServicesAsync();
            gameConnection.State = ClientConnectionState.Matchmaking;

            // Create session options (using GameManager.Instance.MaxNbOfPlayer for max players).
            var options = gameConnection.CreateSessionOptions();
            var networkHandler = new NetworkHandler();
            options.WithNetworkHandler(networkHandler);

            // Try to join the session with the given sessionId, or create it if it doesn't exist.
            gameConnection.Session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options);

            // Retrieve connection endpoints from the network handler.
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;

            return gameConnection;
        }

        //public static async Task<IHostSession> CreateNewServerSessionAsync(SessionOptions sessionOptions, ServerSessionSettings sessionSettings)
        //{
        //    // Ensure services are initialized.
        //    await StartServicesAsync();
        //    var gameConnection = new ClientConnection(IP, Port, IsClientLocal);
        //    SessionOptions options = new SessionOptions { MaxPlayers = sessionSettings.MaxPlayers }.WithDirectNetwork(ClientConnection.IP, IP, Port);
        //    IHostSession serverSession = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
        //    UnityEngine.Debug.Log($"Server created session with code: {serverSession.Id}");
        //    return serverSession;
        //}

        public async Task<SessionTransportHelper> CreateServerSessionAsync(SessionOptions sessionOptions)
        {
            //var connection = new SessionTransportHelper(IP, Port, IsClientLocal);

            //var options = connection.CreateSessionOptions();
            //options.MaxPlayers = sessionOptions.MaxPlayers;
            //var networkHandler = new NetworkHandler();
            //options.WithNetworkHandler(networkHandler);

            //connection.Session = await MultiplayerService.Instance.CreateSessionAsync(options);
            //UnityEngine.Debug.Log($"[ClientConnection] Server created session with code: {connection.Session.Id}");

            //connection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            //connection.ListenEndpoint = await networkHandler.ListenEndpoint;
            //connection.SessionConnectionType = await networkHandler.SessionConnectionType;

            //return connection;

            try
            {
                await StartServicesAsync();

                UnityEngine.Debug.Log($"[SessionTransportHelper] Starting CreateServerSessionAsync with IP: {IP}, Port: {Port}, IsClientLocal: {IsClientLocal}");

                // Create a new instance of SessionTransportHelper with the current settings.
                var connection = new SessionTransportHelper(IP, Port, IsClientLocal);

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

                // Attempt to create the session.
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
                throw; // or return null
            }
        }

        /// <summary>
        /// Generates a random uppercase-alphanumeric string (e.g. "IJVK4S").
        /// </summary>
        public static string GenerateRandomSessionCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new System.Random();
            var result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }
        public async Task<SessionTransportHelper> JoinSessionByIdAsync(string sessionCode, CancellationToken cancellationToken)
        {
            // Create a new instance with current settings.
            var gameConnection = new SessionTransportHelper(IP, Port, IsClientLocal);
            //await StartServicesAsync();
            gameConnection.State = ClientConnectionState.Matchmaking;

            // Create join options (and attach your network handler).
            var joinOptions = new JoinSessionOptions();
            var networkHandler = new NetworkHandler();
            joinOptions.WithNetworkHandler(networkHandler);

            // Attempt to join the session with the given sessionCode.
            gameConnection.Session = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionCode, joinOptions);
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;

            UnityEngine.Debug.Log($"Client joined session with code: {gameConnection.Session.Id}");
            return gameConnection;
        }

        /// <summary>
        /// Uses Unity's Multiplayer Service to join matchmaking.
        /// </summary>
        public async Task<SessionTransportHelper> JoinOrCreateMatchmakerGameAsync(CancellationToken cancellationToken)
        {
            // Create a new instance with current settings.
            var gameConnection = new SessionTransportHelper(IP, Port, IsClientLocal);
            await StartServicesAsync();
            gameConnection.State = ClientConnectionState.Matchmaking;

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

        /// <summary>
        /// Uses Unity Services Matchmaker to find and join (or create) a session.
        /// </summary>
        /// <param name="matchOptions">Matchmaking queue options</param>
        /// <param name="sessionOptions">Session creation options (max players, etc.)</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>This helper, or null on error</returns>
        public async Task<SessionTransportHelper> MatchmakeSessionAsync(MatchmakerOptions matchOptions,
            SessionOptions sessionOptions,
            CancellationToken token = default)
        {
            try
            {
                UnityEngine.Debug.Log("[SessionTransportHelper] Starting matchmaking...");

                // 1. Attempt to matchmake a session (join or create automatically).
                Session = await MultiplayerService.Instance.MatchmakeSessionAsync(matchOptions, sessionOptions, token);
                if (Session == null)
                {
                    UnityEngine.Debug.LogError("[SessionTransportHelper] Matchmaking returned a null session.");
                    return null;
                }

                SessionID = Session.Id;
                UnityEngine.Debug.Log($"[SessionTransportHelper] Matchmade session. Session ID: {Session.Id}, Code: {Session.Code}");

                return this;
            }
            catch (SessionException ex)
            {
                UnityEngine.Debug.LogError($"[SessionTransportHelper] Error during matchmaking: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Uses Unity Services Multiplayer to find or create a session via QuickJoin.
        /// </summary>
        /// <param name="quickJoinOptions">Search options for quick join</param>
        /// <param name="sessionOptions">Options for creating a new session if none is found</param>
        /// <returns>This helper, or null on error</returns>
        public async Task<SessionTransportHelper> QuickJoinSessionAsync(QuickJoinOptions quickJoinOptions,
            SessionOptions sessionOptions)
        {
            try
            {
                UnityEngine.Debug.Log("[SessionTransportHelper] Starting quick join matchmaking...");
                Session = await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);

                if (Session == null)
                {
                    UnityEngine.Debug.LogError("[SessionTransportHelper] Quick join returned a null session.");
                    return null;
                }

                SessionID = Session.Id;
                UnityEngine.Debug.Log($"[SessionTransportHelper] Quick-joined session. Session ID: {Session.Id}, Code: {Session.Code}");


                return this;
            }
            catch (SessionException ex)
            {
                UnityEngine.Debug.LogError($"[SessionTransportHelper] Error during quick join: {ex.Message}");
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

        public SessionOptions CreateSessionOptions()
        {
            // Assume GameManager.Instance.MaxNbOfPlayer is available.
            SessionOptions options = new SessionOptions { MaxPlayers = 3 }; //CAREFUL HERE MOTHERFUCKER
            return options.WithDirectNetwork(IP, IP, Port);
        }
    }
}
