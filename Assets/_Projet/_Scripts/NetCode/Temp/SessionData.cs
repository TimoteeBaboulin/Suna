using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;
using System.Runtime.CompilerServices;
using Unity.Services.Multiplayer;
using static System.Collections.Specialized.BitVector32;
using GameNetwork.Utils;

namespace GameNetwork
{
    public class SessionData : INotifyBindablePropertyChanged
    {
        public enum LoadingSteps
        {
            StartLoading,
            InitializeConnection,
            LookingForMatch,
            WaitingForPlayers,
            CreateWorld,
            WaitingConnection,
            LoadGameScene,
            LoadResourcesScene,
            LoadServer,
            LoadClient,
            WorldReplication,
            WaitingOnPlayer,
            LoadingDone,

            UnloadingGame,
            DisconnectingClient,
            UnloadingWorld,
            UnloadingGameScene,
            UnloadingResourcesScene,
            BackToMainMenu,

            NotLoading,
        }

        public struct LoadingStepHelper
        {
            public readonly string Text;
            public readonly float Start;
            public readonly float End;

            public LoadingStepHelper(string text, float start, float end)
            {
                Text = text;
                Start = start;
                End = end;
            }
        }

        static readonly Dictionary<LoadingSteps, LoadingStepHelper> k_LoadingSteps = new()
        {
            { LoadingSteps.StartLoading , new LoadingStepHelper("InitializeLoading...", 0f, 0f) },
            { LoadingSteps.InitializeConnection , new LoadingStepHelper("Initializing connection...", 0.1f, 0.1f) },
            { LoadingSteps.WaitingForPlayers , new LoadingStepHelper("Looking for other players...", 0.11f, 0.11f) },
            { LoadingSteps.LookingForMatch , new LoadingStepHelper("Looking for a match session...", 0.12f, 0.12f) },
            { LoadingSteps.CreateWorld , new LoadingStepHelper("Creating entity worlds...", 0.15f, 0.15f) },
            { LoadingSteps.WaitingConnection , new LoadingStepHelper("Waiting for Client connection...", 0.2f, 0.2f) },
            { LoadingSteps.LoadGameScene , new LoadingStepHelper("Loading Gameplay scene...", 0.3f, 0.5f) },
            { LoadingSteps.LoadResourcesScene , new LoadingStepHelper("Loading Resources scene...", 0.6f, 0.6f) },
            { LoadingSteps.LoadServer , new LoadingStepHelper("Loading Server world...", 0.6f, 0.7f) },
            { LoadingSteps.LoadClient , new LoadingStepHelper("Loading Client world...", 0.7f, 0.8f) },
            { LoadingSteps.WorldReplication , new LoadingStepHelper("Replicating world...", 0.8f, 0.9f) },
            { LoadingSteps.WaitingOnPlayer , new LoadingStepHelper("Waiting for Player spawn...", 0.9f, 0.9f) },
            { LoadingSteps.LoadingDone , new LoadingStepHelper("Starting gameplay...", 1f, 1f) },
            { LoadingSteps.UnloadingGame , new LoadingStepHelper("Leaving gameplay...", 0f, 0f) },
            { LoadingSteps.DisconnectingClient , new LoadingStepHelper("Disconnecting Client...", 0.1f, 0.1f) },
            { LoadingSteps.UnloadingWorld , new LoadingStepHelper("Disposing entity worlds...", 0.1f, 0.2f) },
            { LoadingSteps.UnloadingGameScene , new LoadingStepHelper("Unloading Gameplay scene...", 0.2f, 0.5f) },
            { LoadingSteps.UnloadingResourcesScene , new LoadingStepHelper("Unloading Resources scene...", 0.5f, 0.9f) },
            { LoadingSteps.BackToMainMenu , new LoadingStepHelper("Opening Main menu...", 1f, 1f) },

            { LoadingSteps.NotLoading , new LoadingStepHelper("_", 0f, 0f) },
        };

        public static SessionData Instance { get; private set; } = null!;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad() => Instance = new SessionData();

        SessionData()
        {
            loadingProgress = 0.0f;
            CurrentPlayerCount = 0;
            SessionMaxPlayers = 0;
            SessionID = string.Empty;
        }

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        void Notify([CallerMemberName] string property = "") =>
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));

        public void UpdateLoading(LoadingSteps step, float stepProgress = 0f)
        {
            var currentStep = k_LoadingSteps[step];
            LoadingProgress = currentStep.Start + stepProgress * (currentStep.End - currentStep.Start);
            LoadingStatusText = currentStep.Text;
            Debug.Log(loadingStatusText);
        }

        public int CurrentPlayerCount { get; private set; }
        public int SessionMaxPlayers { get; private set; }
        public string SessionID { get; private set; } = "SESSION01";

        public ISession Session { get; private set; }


        public void UpdateSessionState(int currentPlayerCount, int maxPlayers, ISession session)
        {
            CurrentPlayerCount = currentPlayerCount;
            SessionMaxPlayers = maxPlayers;
            Session = session;
            SessionID = session.Id;
            Debug.Log($"[LoadingData] Session state updated: {CurrentPlayerCount}/{SessionMaxPlayers} players, SessionID: {SessionID}");
        }

        float loadingProgress;
        public const string LoadingProgressPropertyName = nameof(LoadingProgress);
        [CreateProperty]
        public float LoadingProgress
        {
            get => loadingProgress;
            set
            {
                loadingProgress = value;
                Notify();
            }
        }
        string loadingStatusText;
        public const string LoadingStatusTextPropertyName = nameof(LoadingStatusText);
        [CreateProperty]
        public string LoadingStatusText
        {
            get => loadingStatusText;
            set
            {
                if (loadingStatusText == value)
                    return;
                loadingStatusText = value;
                Notify();
            }
        }
    }
}
