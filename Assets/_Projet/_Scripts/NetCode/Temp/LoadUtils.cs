using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameManager;
using static Unity.NetCode.ClientServerBootstrap;

namespace GameNetwork.Utils
{
    public class LoadUtils
    {
        public static void CreateEntityWorlds()
        {
            SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.CreateWorld);
            DestroySimulationWorld();

            ClientTransportHelper.ClientWorld = null;
            ClientTransportHelper.ServerWorld = null;
            switch (RequestedPlayType)
            {
                case PlayType.ClientAndServer:
                    //role = NetworkRole.Host;
                    ClientTransportHelper.ClientWorld = CreateClientWorld("ClientWorld");
                    ClientTransportHelper.ServerWorld = CreateServerWorld("ServerWorld");
                    if (ClientTransportHelper.ServerWorld == null)
                    {
                        UnityEngine.Debug.LogError("Server world creation failed in ClientAndServer mode.");
                    }
                    ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {RequestedPlayType}");
                    UnityEngine.Debug.Log($"Connection Request Type {RequestedPlayType}");
                    //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                    break;
                case PlayType.Server:
                    //role = NetworkRole.Server;
                    ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {RequestedPlayType}");
                    UnityEngine.Debug.Log($"Connection Request Type {RequestedPlayType}");
                    UnityEngine.Debug.Log($"serverWorld in CreateEntityWorlds {ServerWorld}");
                    //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                    break;
                case PlayType.Client:
                    //role = NetworkRole.Client;
                    ClientTransportHelper.ClientWorld = CreateClientWorld("ClientWorld");
                    ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {RequestedPlayType}");
                    UnityEngine.Debug.Log($"Connection Request Type {RequestedPlayType}");
                    //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                    break;
                default:
                    UnityEngine.Debug.LogError("ConnectionHandlerNew: No valid role specified.");
                    break;
            }
        }

        public static void CreateEntityWorlds(ISession session)
        {
            SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.CreateWorld);
            DestroySimulationWorld();

            switch (RequestedPlayType)
            {
                case PlayType.ClientAndServer:
                    //role = NetworkRole.Host;
                    ClientTransportHelper.ClientWorld = CreateClientWorld("ClientWorld");
                    ClientTransportHelper.ServerWorld = CreateServerWorld("ServerWorld");
                    ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {RequestedPlayType}");
                    UnityEngine.Debug.Log($"Connection Request Type {RequestedPlayType}");
                    //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                    break;
                case PlayType.Server:
                    //role = NetworkRole.Server;
                    ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {RequestedPlayType}");
                    UnityEngine.Debug.Log($"Connection Request Type {RequestedPlayType}");
                    //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                    break;
                case PlayType.Client:
                    //role = NetworkRole.Client;
                    ClientTransportHelper.ClientWorld = CreateClientWorld("ClientWorld");
                    ServerConsole.Log(ServerConsole.LogType.Info, $"Connection Request Type {RequestedPlayType}");
                    UnityEngine.Debug.Log($"Connection Request Type {RequestedPlayType}");
                    //NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor(role);
                    break;
                default:
                    UnityEngine.Debug.LogError("ConnectionHandlerNew: No valid role specified.");
                    break;
            }
        }

        public static async Task LoadGameplayAsync(World server, World client)
        {
            if (server != null)
            {
                await SubScenesLoading(server, SessionData.LoadingSteps.LoadServer);
            }
            if (client != null)
            {
                await SubScenesLoading(client, SessionData.LoadingSteps.LoadClient);
            }
        }

        private static async Task SubScenesLoading(World world, SessionData.LoadingSteps step)
        {
            if (world == null) { return; }
            UnityEngine.Debug.Log($"[SubScenesLoading] Querying SceneReferences in world: {world.Name}");
            SessionData.Instance.UpdateLoading(step);

            using var scenesQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SceneReference>());
            using var scenesLeftToLoad = scenesQuery.ToEntityListAsync(Allocator.Persistent, out var handle);

            handle.Complete();

            int totalScenes = scenesLeftToLoad.Length;
            while (scenesLeftToLoad.Length > 0)
            {
                for (int i = 0; i < scenesLeftToLoad.Length; i++)
                {
                    var sceneEntity = scenesLeftToLoad[i];
                    if (SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
                    {
                        scenesLeftToLoad.RemoveAt(i);

                        float progress = 1f - (float)scenesLeftToLoad.Length / totalScenes;
                        SessionData.Instance.UpdateLoading(step, progress);
                        break;
                    }
                }

                await Awaitable.NextFrameAsync();
            }
        }
        public static async Task LoadSceneAsync(string sceneName, SessionData.LoadingSteps step)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
                return;
            var sceneLoading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            UpdateLoadingStateAsync(step, sceneLoading);
            await sceneLoading;
        }

        public static async Task LoadSceneAsync(int sceneID, SessionData.LoadingSteps step)
        {
            if (SceneManager.GetSceneByBuildIndex(sceneID).isLoaded)
                return;
            var sceneLoading = SceneManager.LoadSceneAsync(sceneID, LoadSceneMode.Single);
            UpdateLoadingStateAsync(step, sceneLoading);
            await sceneLoading;
        }

        public static async void ReturnToMainMenuAsync()
        {
            SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.UnloadingGame);
            await DisconnectAndUnloadWorlds();
        }

        public static void ResetAllCharacterComponents()
        {
            foreach (var world in World.All)
            {
                var em = world.EntityManager;
                if (!world.IsCreated)
                    continue;

                using var query = em.CreateEntityQuery(ComponentType.ReadOnly<CharacterComponent>());
                var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var e in entities)
                {
                    em.RemoveComponent<CharacterComponent>(e);
                    em.RemoveChunkComponent<ClientComponent>(e);
                }
                entities.Dispose();
            }
        }

        private static async Task LeaveSessionAsync()
        {
            if (ClientTransportHelper.instance.Session != null)
            {
                ClientTransportHelper.instance.Session.RemovedFromSession -= OnSessionLeft;

                if (ClientTransportHelper.instance.Session.IsHost ||
                    RequestedPlayType == PlayType.ClientAndServer)
                {
                    ClientTransportHelper.SessionID = null;
                }

                if (ClientTransportHelper.instance.Session.IsHost ||
                    RequestedPlayType == PlayType.ClientAndServer)
                {
                    UnityEngine.Debug.Log($"[LeaveSessionAsync] confirmation session deleted.");
                    await ClientTransportHelper.instance.Session.AsHost().DeleteAsync();
                }
                else
                {
                    await ClientTransportHelper.instance.Session.LeaveAsync();
                }

                ClientTransportHelper.instance.Session = null;
            }
        }

        public static async Task DisconnectAndUnloadWorlds()
        {
            ClientTransportHelper.State = ClientConnectionState.NotConnected;

            //bool requestedDisconnect = false;
            foreach (var world in World.All)
            {
                if (world.IsClient())
                {
                    using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
                    if (query.TryGetSingletonEntity<NetworkId>(out var networkId))
                    {
                        //requestedDisconnect = true;
                        world.EntityManager.AddComponentData(networkId, new NetworkStreamRequestDisconnect());
                    }
                }
            }

            //if (requestedDisconnect)
            //    await Awaitable.NextFrameAsync();

            if (ClientTransportHelper.instance.Session != null)
            {
                await LeaveSessionAsync();
            }
            await DestroyGameSessionWorlds();
            await UnloadScenesAsync("MultiplayerTest");
        }

        public static async Task LeaveSessionAndDestroyWorldsOnly()
        {
            ClientTransportHelper.State = ClientConnectionState.NotConnected;

            foreach (var world in World.All)
            {
                if (world.IsClient())
                {
                    using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
                    if (query.TryGetSingletonEntity<NetworkId>(out var networkId))
                    {
                        world.EntityManager.AddComponentData(networkId, new NetworkStreamRequestDisconnect());
                    }
                }
            }

            await LeaveSessionAsync();
            await DestroyGameSessionWorlds();
        }

        public static Task QuitAsync() => DisconnectAndUnloadWorlds();

        private static void OnSessionLeft()
        {
            ClientTransportHelper.instance = null;
            ReturnToMainMenuAsync();
        }
        public static Task DestroyGameSessionWorlds()
        {
            var nets = new List<World>();
            for (int i = 0; i < World.All.Count; i++)
            {
                var world = World.All[i];
                if (world.IsClient() || world.IsServer())
                    nets.Add(world);
            }

            if (nets.Count > 0)
            {
                foreach (var world in nets)
                {
                    world.EntityManager.CompleteAllTrackedJobs();
                    world.Dispose();
                }
            }

            return Task.CompletedTask;
        }

        private static async void UpdateLoadingStateAsync(SessionData.LoadingSteps step, AsyncOperation loadingTask)
        {
            while (loadingTask != null && !loadingTask.isDone)
            {
                SessionData.Instance.UpdateLoading(step, loadingTask.progress);
                await Awaitable.NextFrameAsync();
            }
        }
        private static void DestroySimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
            }
        }

        public static async Task UnloadScenesAsync(string sceneName)
        {
            SessionData.Instance.UpdateLoading(SessionData.LoadingSteps.UnloadingWorld);

            var gameplay = SceneManager.GetSceneByName(sceneName);
            if (gameplay.IsValid() && gameplay != SceneManager.GetActiveScene())
            {
                UnityEngine.Debug.Log($"Unloading Scene {sceneName}");
                var unloadScene = SceneManager.UnloadSceneAsync(gameplay);
                UpdateLoadingStateAsync(SessionData.LoadingSteps.UnloadingGameScene, unloadScene);
                await unloadScene;
            }
        }

        public static void RestartServer()
        {
            string exePath = Application.dataPath.Replace("_Data", ".exe");
            UnityEngine.Debug.Log($"[ServerRestartHelper] Attempting to restart server using path: {exePath}");

            try
            {
                Process.Start(exePath);
                Application.Quit();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[ServerRestartHelper] Failed to restart server: {ex}");
            }
        }
    }
}



