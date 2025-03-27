using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [Header("HUD default Document")]
    [SerializeField] private UIDocument _mainMenuDocument;
    [SerializeField] private UIDocument _settingsMenuDocument;

    [Header("Connection Helper")]
    //[SerializeField] private ConnectionManager connectionManager;

    private VisualElement _mainMenu;
    private VisualElement _settingsMenu;

    // Main Menu Elements
    private Button _playButton;
    private Button _settingsButton;
    private Button _quitButton;

    // Settings Menu Elements
    private Button _exitButton;
    private Slider _sensitivitySlider;
    private FloatField _sensitivityField;

    [SerializeField] private SceneID _sceneLoadOnPlay = SceneID.MultipayerTest;

    private void Awake()
    {
        if (_mainMenuDocument != null && _settingsMenuDocument != null)
        {
            // Get the root visual elements
            _mainMenu = _mainMenuDocument.rootVisualElement;
            _settingsMenu = _settingsMenuDocument.rootVisualElement;

            // Disable the settings menu
            _settingsMenu.style.opacity = 0;
            _settingsMenu.SetEnabled(false);
            _settingsMenuDocument.sortingOrder = -1;

            // Get the main menu elements
            _playButton = _mainMenu.Q<Button>("PlayButton");
            _playButton.clicked += OnPlayButton_Click;

            _settingsButton = _mainMenu.Q<Button>("SettingsButton");
            _settingsButton.clicked += OnSettingsButton_Click;

            _quitButton = _mainMenu.Q<Button>("QuitButton");
            _quitButton.clicked += OnQuitButton_Click;


            // Get the settings menu elements
            _exitButton = _settingsMenu.Q<Button>("ExitButton");
            _exitButton.clicked += OnExitButton_Click;

            _sensitivitySlider = _settingsMenu.Q<Slider>("SensitivitySlider");
            _sensitivitySlider.RegisterValueChangedCallback(OnSensitivitySlider_ValueChanged);

            _sensitivityField = _settingsMenu.Q<FloatField>("SensitivityField");
            _sensitivityField.RegisterValueChangedCallback(OnSensitivityField_ValueChanged);
        }
    }

    private void OnPlayButton_Click()
    {
        //if (connectionManager != null)
        //{
            GameManager.Instance.PlayMatchmaking();
            SceneManager.LoadScene((int)_sceneLoadOnPlay);
        //}
    }

    private void OnSettingsButton_Click()
    {
        _mainMenu.style.opacity = 0;
        _mainMenu.SetEnabled(false);
        _mainMenuDocument.sortingOrder = -1;

        _settingsMenu.style.opacity = 1;
        _settingsMenu.SetEnabled(true);
        _settingsMenuDocument.sortingOrder = 0;

        if (ClientServerBootstrap.ServerWorld == null)
        {
            MainMenuLinkSystem mainMenuLinkSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<MainMenuLinkSystem>();
            if (mainMenuLinkSystem.TryGetClientSettings(out ClientSettingsComponent clientSettings))
            {
                _sensitivitySlider.value = clientSettings.Sensivity;
            }
        }
    }

    private void OnQuitButton_Click()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    private void OnExitButton_Click()
    {
        if (ClientServerBootstrap.ServerWorld == null)
        {
            MainMenuLinkSystem mainMenuLinkSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<MainMenuLinkSystem>();
            if (mainMenuLinkSystem.TryGetClientSettings(out ClientSettingsComponent clientSettings))
            {
                clientSettings.Sensivity = _sensitivitySlider.value;
                mainMenuLinkSystem.UpdateClientSettings(clientSettings);
            }
        }

        _settingsMenu.style.opacity = 0;
        _settingsMenu.SetEnabled(false);
        _settingsMenuDocument.sortingOrder = -1;

        _mainMenu.style.opacity = 1;
        _mainMenu.SetEnabled(true);
        _mainMenuDocument.sortingOrder = 0;
    }

    private void OnSensitivitySlider_ValueChanged(ChangeEvent<float> evt)
    {
        _sensitivityField.value = evt.newValue;
    }

    private void OnSensitivityField_ValueChanged(ChangeEvent<float> evt)
    {
        float value = Mathf.Clamp(evt.newValue, _sensitivitySlider.lowValue, _sensitivitySlider.highValue);
        _sensitivitySlider.value = value;
        _sensitivityField.value = value;
    }
}
