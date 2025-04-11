using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [Header("Main Menu Component")]
    // Assets
    [SerializeField] private VisualTreeAsset _settingsMenuAsset;

    // Main Elements
    private UIDocument _document;
    private VisualElement _root;
    private Button _playButton;
    private Button _settingsButton;
    private Button _quitButton;
    private VisualElement _container;

    [Header("Connection Helper")]
    [SerializeField] private ConnectionManager connectionManager;

    [SerializeField] private SceneID _sceneLoadOnPlay = SceneID.MultipayerTest;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();

        // Get the root visual elements
        _root = _document.rootVisualElement;
        _container = _root.Q<VisualElement>("Container");

        // Get the main menu elements
        _playButton = _root.Q<Button>("PlayButton");
        _playButton.clicked += OnPlayButton_Click;

        _settingsButton = _root.Q<Button>("SettingsButton");
        _settingsButton.clicked += OnSettingsButton_Click;

        _quitButton = _root.Q<Button>("QuitButton");
        _quitButton.clicked += OnQuitButton_Click;
    }

    private void Start()
    {
        // Unlock Cursor at start
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private async void OnPlayButton_Click()
    {
        _playButton.SetEnabled(false);
        await GameManager.Instance.Play();
        _playButton.SetEnabled(true);
    }

    private void OnSettingsButton_Click()
    {
        // Save and Close Settings if Settings Menu was open
        if (gameObject.TryGetComponent(out SettingsMenuController settingsMenuController))
        {
            SaveAndCloseSettings(settingsMenuController);
        }

        // Instantiate Settings Menu
        settingsMenuController = gameObject.AddComponent<SettingsMenuController>();
        settingsMenuController.root = _settingsMenuAsset.Instantiate().Children().First();
        _container.Add(settingsMenuController.root);
    }
    private void SaveAndCloseSettings(SettingsMenuController settingsMenuController)
    {
        settingsMenuController.SaveAndCloseSettings();
        Destroy(settingsMenuController);
    }

    private void OnQuitButton_Click()
    {
        Application.Quit();
#if UNITY_EDITOR
        // If in Editor Mode, stop Play Mode
        EditorApplication.isPlaying = false;
#endif
    }
}
