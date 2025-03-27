using GameNetwork.Utils;
using System.Threading;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GlobalGameState
    {
        MainMenu,
        Loading,
        InGame
    }

    public int MaxNbOfPlayer = 4;

    // This will store the connection settings returned by the ConnectionHandlerNew.
    private ClientConnection clientConnectionSettings;
    private CancellationTokenSource loadingToken;
    private ConnectionHandlerNew connectionHandler;

    protected override void Awake()
    {
        base.Awake();
        connectionHandler = FindFirstObjectByType<ConnectionHandlerNew>();
        loadingToken = new CancellationTokenSource();
    }

    /// <summary>
    /// Initiates matchmaking via the connection handler.
    /// Since ConnectionHandlerNew already creates the entity worlds,
    /// GameManager just updates the game state (or loads the gameplay scene).
    /// </summary>
    public async void PlayMatchmaking()
    {
        try
        {
            clientConnectionSettings = await connectionHandler.ConnectMatchmakingAsync(loadingToken.Token);

            if (clientConnectionSettings == null)
            {
                Debug.LogError("GameManager: Client connection settings are null.");
                return;
            }

            Debug.Log($"GameManager: Matchmaking complete. ClientEndpoint: {clientConnectionSettings.ConnectEndpoint}, " +
                      $"ServerEndpoint: {clientConnectionSettings.ListenEndpoint}");

            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during matchmaking: {ex}");
        }

        // Use ConnectionHandlerNew to perform matchmaking and set up the worlds.


        // Now that worlds are created and subscenes loaded by ConnectionHandlerNew,
        // update your global game state or load the gameplay scene.
        //GameSettings.Instance.GameState = GlobalGameState.InGame;
        // Optionally, if your gameplay scene is not already loaded, you can load it additively or normally:
        // SceneManager.LoadScene("GameplayScene");

        // Any further world management is handled by ConnectionHandlerNew.
    }
}