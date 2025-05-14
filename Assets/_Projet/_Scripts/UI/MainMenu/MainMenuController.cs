using GameNetwork;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [Header("Main Menu Component")]
    // Assets
    [SerializeField] private VisualTreeAsset _settingsMenuAsset;
    [SerializeField] private GameManager _gameManager;

    // Main Elements
    private UIDocument _document;
    private VisualElement _root;
    private Button _playButton;
    private Button _settingsButton;
    private Button _quitButton;
    private VisualElement _container;

    private VisualElement _connectionFeedback;
    private Label _connectionFeedbackLabel;
    private VisualElement _connectionFeedbackFill;
    private bool _sessionDataFound = false;


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

        _connectionFeedback = _root.Q<VisualElement>("ConnectionFeedback");
        _connectionFeedbackLabel = _connectionFeedback.Q<Label>();
        _connectionFeedbackFill = _connectionFeedback.Q<VisualElement>("Fill");

        UIDocumentUtils.SetActive(ref _connectionFeedback, false);
    }

    private void Start()
    {
        // Unlock Cursor at start
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        //SoundManager.Instance.Play("Management", "StopAll", Vector3.zero);
        SoundManager.Instance.Play("Music", "MainMenu", Vector3.zero);
    }

    private void Update()
    {
        if (_sessionDataFound) return;

        if (SessionData.Instance != null)
        {
            Debug.Log("SessionData.Instance found");
            _sessionDataFound = true;
            _connectionFeedback.dataSource = SessionData.Instance;
            SessionData.Instance.propertyChanged += OnSessionDataPropertyChanged;
        }
    }

    private void OnDestroy()
    {
        SessionData.Instance.propertyChanged -= OnSessionDataPropertyChanged;
    }

    private void OnSessionDataPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
    {
        UIDocumentUtils.SetActive(ref _connectionFeedback, true);
        _connectionFeedbackLabel.text = SessionData.Instance.LoadingStatusText;
        _connectionFeedbackFill.style.width = UIDocumentUtils.PercentLength(SessionData.Instance.LoadingProgress * 100f);
    }

    private async void OnPlayButton_Click()
    {
        _playButton.SetEnabled(false);
        await _gameManager.Play();
        _playButton.SetEnabled(true);

        //Debug.Log("HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH");
        //SoundManager.Instance.Play("Management", "StopAll", Vector3.zero);
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
