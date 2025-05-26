using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
using static Unity.NetCode.ClientServerBootstrap;

namespace GameNetwork.Utils
{
    public enum ClientConnectionState
    {
        NotConnected,
        Connecting,
        Connected,
        Matchmaking,
    }

    [System.Serializable]
    public class SessionInfoData
    {
        public int Network;          
        public string Ip;            
        public int Port;            
        public string RelayJoinCode; 
    }

    public class ClientTransportHelper
    {
        public ISession Session { get; set; }
        public NetworkEndpoint ListenEndpoint { get; private set; }
        public NetworkEndpoint ConnectEndpoint { get; private set; }
        public NetworkType SessionConnectionType { get; private set; }

        public static string SessionID { get; set; }
        public static string CurrentIP { get; set; } = GetLocalIPAddress();
        public static ushort CurrentPort { get; set; }
        public static bool isClientLocal { get; set; } = false;
        public static ClientConnectionState State = ClientConnectionState.NotConnected;
        public static int MaxNbOfPlayers = 2;
        public static bool isRelease = true;
        public static World ClientWorld { get; set; } = null;
        public static World ServerWorld { get; set; } = null;

        public static ClientTransportHelper instance { get; set; }


        public static ushort GetAvailablePort()
        {
            while (true)
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Any, 0);
                tcpListener.Start();
                int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;

                try
                {
                    using (UdpClient udpClient = new UdpClient(port))
                    {
                        tcpListener.Stop();

                        string ruleName = $"Allow_Port_{port}";
                        string command = $"netsh advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol=TCP localport={port}";
                        try
                        {
                            Process.Start(new ProcessStartInfo("cmd.exe", "/c " + command)
                            {
                                Verb = "runas", 
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                            Console.WriteLine($"Firewall exception added for port {port}.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error adding firewall exception: {ex.Message}");
                        }

                        return (ushort)port;
                    }
                }
                catch (SocketException)
                {
                    tcpListener.Stop();
                }
            }
        }
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
#if UNITY_SERVER
                UnityEngine.Debug.Log($"[SessionTransportHelper] Starting CreateServerSessionAsync with IP: {CurrentIP}, Port: {CurrentPort}");
#endif

                instance = new ClientTransportHelper();

                var options = instance.CreateSessionOptions();
                options.Name = sessionOptions.Name;

                var networkHandler = new NetworkHandler();
                options.WithNetworkHandler(networkHandler);
#if UNITY_SERVER

                UnityEngine.Debug.Log("[SessionTransportHelper] NetworkHandler attached to session options.");
#endif

                IHostSession hostSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                if (hostSession == null)
                {
#if UNITY_SERVER

                    UnityEngine.Debug.LogError("[SessionTransportHelper] CreateSessionAsync returned null host session!");
#endif

                    var portProperty = new SessionProperty(AutoConnectPort.ToString(), VisibilityPropertyOptions.Public);
                    UnityEngine.Debug.Log($"[CreateSession] Port confirmation {AutoConnectPort}");
                    hostSession.SetProperty("port", portProperty);
                    return instance;
                }

                instance.Session = hostSession;
#if UNITY_SERVER

                UnityEngine.Debug.Log($"[SessionTransportHelper] Server created session with official ID: {hostSession.Id}");
                UnityEngine.Debug.Log($"[SessionTransportHelper] MaxPlayer Host: {hostSession.MaxPlayers}, Max Player session Options {sessionOptions.MaxPlayers}");
#endif

                instance.ConnectEndpoint = await networkHandler.ConnectEndpoint;
                instance.ListenEndpoint = await networkHandler.ListenEndpoint;
                instance.SessionConnectionType = await networkHandler.SessionConnectionType;
#if UNITY_SERVER

                UnityEngine.Debug.Log($"[SessionTransportHelper] Endpoints retrieved: " +
                    $"ConnectEndpoint={instance.ConnectEndpoint}, " +
                    $"ListenEndpoint={instance.ListenEndpoint}, " +
                    $"SessionConnectionType={instance.SessionConnectionType}");
#endif

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
            // Initialize the instance of ClientTransportHelper
            instance = new ClientTransportHelper();

            // Start necessary services (assuming StartServicesAsync is a method to initialize services)
            await StartServicesAsync();

            var joinOptions = new JoinSessionOptions();
            var networkHandler = new NetworkHandler();
            joinOptions.WithNetworkHandler(networkHandler);

            // Join the session by session ID
            instance.Session = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionID, joinOptions);

            //Debug.Log($"instance.Session.Properties.Count: {instance.Session.Properties.Count}");

            //// Variable to hold the port to return
            //int portToReturn = 0;

            //// Iterate through session properties to check the port
            //foreach (var property in instance.Session.Properties)
            //{
            //    Debug.Log($"item: {property.Value.Value}");
            //    if (property.Value.Value is string jsonString)
            //    {
            //        SessionInfoData sessionData = JsonUtility.FromJson<SessionInfoData>(jsonString);
            //        Debug.Log($"Port: {sessionData.Port}");

            //        if (sessionData.Port.ToString() == instance.Session.Name)  
            //        {
            //            Debug.Log($"match Port: {sessionData.Port} vs {instance.Session.Name} ");
            //            portToReturn = sessionData.Port;
            //            break;  
            //        }
            //    }
            //}

            //if (portToReturn == 0)
            //{
            //    return null;
            //}

            instance.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            instance.ListenEndpoint = await networkHandler.ListenEndpoint;
            instance.SessionConnectionType = await networkHandler.SessionConnectionType;

            UnityEngine.Debug.Log($"Client joined session with code: {instance.Session.Id}");
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

            UnityEngine.Debug.Log($"Client reconnected to session with id: {gameConnection.Session.Id}");
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
                UnityEngine.Debug.Log("[SessionTransportHelper] Starting matchmaking...");

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

        public async Task<ClientTransportHelper> QuickJoinSessionAsync(QuickJoinOptions quickJoinOptions,
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
            int maxPlayers = RequestedPlayType == PlayType.Server ? MaxNbOfPlayers + 1 : MaxNbOfPlayers;
            UnityEngine.Debug.Log($"maxPlayerServer {maxPlayers}");
            var options = new SessionOptions
            {
                MaxPlayers = maxPlayers                               
            };
            UnityEngine.Debug.Log($"[CreateSession] Port confirmation currentPort {CurrentPort} vs {AutoConnectPort}");
            return options.WithDirectNetwork(CurrentIP, CurrentIP, CurrentPort);
        }

        public static string GetLocalIPAddress()
        {
            try
            {
                string localIP = null;

                foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(localIP))
                    throw new Exception("No network adapters with an IPv4 address in the system!");

                return localIP;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting local IP: {ex.Message}");
                return "127.0.0.1"; // fallback
            }
        }
    }
}
