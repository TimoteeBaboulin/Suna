using GameNetwork.Utils;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

using UI = UIDocumentUtils;

public class PauseMenuController : MonoBehaviour
{
    // Assets
    [SerializeField] private VisualTreeAsset _settingsMenuAsset;

    // Main Elements
    private UIDocument _document;
    private VisualElement _root;
    private VisualElement _background;
    private VisualElement _tabBar;
    private Button _homeButton;
    private Button _playButton;
    private Button _settingsButton;

    // Input enabling variables
    [SerializeField] private DefaultInputSystem input;

    private void Awake()
    {
        // Initializing elements
        _document = GetComponent<UIDocument>();
        _root = _document.rootVisualElement;
        _background = _root.Q<VisualElement>("Background");
        _tabBar = _background.Q<VisualElement>("TabBar");
        _homeButton = _tabBar.Q<Button>("HomeButton");
        _playButton = _tabBar.Q<Button>("PlayButton");
        _settingsButton = _tabBar.Q<Button>("SettingsButton");

        UI.SetActive(ref _root, false);
    }

    private void Update()
    {
        // For now forced to search it every frame, apparently changes every frame
        CharacterInputSystem system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CharacterInputSystem>();
        if (system != null)
        {
            input = system.input;
        }
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UI.ToggleActive(ref _root);
            ActivateUIInput(UI.IsActive(ref _root));
        }
    }

    private void OnEnable()
    {
        _homeButton.clicked += OnHomeButtonClicked;
        _playButton.clicked += OnPlayButtonClicked;
        _settingsButton.clicked += InstantiateSettings;
    }

    private void OnDisable()
    {
        _homeButton.clicked -= OnHomeButtonClicked;
        _playButton.clicked -= OnPlayButtonClicked;
        _settingsButton.clicked -= InstantiateSettings;
    }

    private void InstantiateSettings()
    {
        // Save and Close Settings if Settings Menu was open
        if (gameObject.TryGetComponent(out SettingsMenuController settingsMenuController))
        {
            SaveAndCloseSettings(settingsMenuController);
        }

        // Instantiate Settings Menu
        settingsMenuController = gameObject.AddComponent<SettingsMenuController>();
        settingsMenuController.root = _settingsMenuAsset.Instantiate().Children().First();
        _background.Add(settingsMenuController.root);
    }



    private void SaveAndCloseSettings(SettingsMenuController settingsMenuController)
    {
        settingsMenuController.SaveAndCloseSettings();
        Destroy(settingsMenuController);
    }

    private async void OnHomeButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            await GameManager.Instance.DisconnectAndUnloadWorlds();
            await LoadUtils.LoadSceneAsync("MainMenu", GameNetwork.SessionData.LoadingSteps.BackToMainMenu);
        }
        //SceneManager.LoadScene("MainMenu");
    }

    private void OnPlayButtonClicked()
    {
        UI.SetActive(ref _root, false);
        ActivateUIInput(false);
    }

    private void ActivateUIInput(bool value)
    {
        if (value)
        {
            input.Player.Disable();
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            input.Player.Enable();
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }
}
