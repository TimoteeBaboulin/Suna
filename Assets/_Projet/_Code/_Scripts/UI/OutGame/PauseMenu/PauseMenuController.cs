using System.Linq;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset _settingsMenuAsset;

    // Pause Menu UI Elements
    private UIDocument _document;
    private VisualElement _root;
    private VisualElement _background;
    private VisualElement _tabBar;
    private Button _homeButton;
    private Button _playButton;
    private Button _settingsButton;

    [SerializeField] private DefaultInputSystem input;
    bool inputFound = false;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        _root = _document.rootVisualElement;
        _background = _root.Q<VisualElement>("Background");
        _tabBar = _background.Q<VisualElement>("TabBar");
        _homeButton = _tabBar.Q<Button>("HomeButton");
        _playButton = _tabBar.Q<Button>("PlayButton");
        _settingsButton = _tabBar.Q<Button>("SettingsButton");

        UIDocumentUtils.SetActive(ref _root, false);
    }

    private void Update()
    {
        CharacterInputSystem system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CharacterInputSystem>();
        if (system != null)
        {
            input = system.input;
            inputFound = true;
        }
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UIDocumentUtils.ToggleActive(ref _root);
            ActivateUIInput(UIDocumentUtils.IsActive(ref _root));
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
        settingsMenuController.SaveSettings();
        settingsMenuController.root.RemoveFromHierarchy();
        Destroy(settingsMenuController);
    }

    private void OnHomeButtonClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void OnPlayButtonClicked()
    {
        UIDocumentUtils.SetActive(ref _root, false);
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
