using GameNetwork.Utils;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

using UI = UIDocumentUtils;

public class PauseMenuController : MonoBehaviour, IUIController
{
    // Assets
    [SerializeField] private VisualTreeAsset _settingsMenuAsset;

    // Main Elements
    private UIDocument _document;
    private VisualElement _root;
    private VisualElement _background;
    private VisualElement _tabBar;
    private VisualElement _container;
    private Button _homeButton;
    private Button _playButton;
    private Button _settingsButton;

    public UICentralController centralController { get => transform.parent.GetComponent<UICentralController>(); }

    private void Awake()
    {
        // Initializing elements
        _document = GetComponent<UIDocument>();
        _root = _document.rootVisualElement;
        _background = _root.Q<VisualElement>("Background");
        _tabBar = _background.Q<VisualElement>("TabBar");
        _container = _background.Q<VisualElement>("Container");
        _homeButton = _tabBar.Q<Button>("HomeButton");
        _playButton = _tabBar.Q<Button>("PlayButton");
        _settingsButton = _tabBar.Q<Button>("SettingsButton");
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
        _container.Add(settingsMenuController.root);
    }



    private void SaveAndCloseSettings(SettingsMenuController settingsMenuController)
    {
        settingsMenuController.SaveAndCloseSettings();
        Destroy(settingsMenuController);
    }

    private async void OnHomeButtonClicked()
    {
        await LoadUtils.QuitAsync();
        await LoadUtils.LoadSceneAsync("MainMenu", GameNetwork.SessionData.LoadingSteps.BackToMainMenu);
    }

    private void OnPlayButtonClicked()
    {
        UI.SetActive(ref _root, false);
        centralController.SetUIActive(this, false);
        centralController.SetCursorActive(false);
        centralController.SetInputActive(true);
    }

    public void SetUIActive(bool value)
    {
        UI.SetActive(ref _root, value);
    }

    public bool IsUIActive()
    {
        return UI.IsActive(ref _root);
    }

    public UICentralController.UIState GetUIState()
    {
        return UICentralController.UIState.PAUSE_MENU;
    }
}
