using GameNetwork.Utils;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ErrorWindowController : MonoBehaviour
{
    // Main Objects
    private UIDocument _document;
    private VisualElement _root;

    // Main Features
    private VisualElement _window;
    private VisualElement _tab;
    private ScrollView _content;
    private Button _closeButton;

    // Window Movement
    private bool _mouseOverTab = false;
    private Vector2 _windowPosition;
    private bool _movementEngaged = false;
    private Vector2 _mouseToWindowPosition = Vector2.zero;

    // To initialize values on first frame
    private bool _firstFrame = true;

    [HideInInspector] public List<string> ErrorsOnStart = new();

    private void Start()
    {
        // Initialize the Window elements
        _document = GetComponent<UIDocument>();
        _root = _document.rootVisualElement;

        _window = _root.Children().First();
        _tab = _root.Q("Tab");
        _content = _root.Q<ScrollView>();
        _closeButton = _root.Q<Button>("Close");

        // Register Callbacks for Window Movement
        _tab.RegisterCallback<MouseEnterEvent>((x) => { _mouseOverTab = true; });
        _tab.RegisterCallback<MouseLeaveEvent>((x) => { _mouseOverTab = false; });
        _tab.RegisterCallback<MouseDownEvent>((x) =>
        {
            _movementEngaged = true;
            _mouseToWindowPosition = _windowPosition - x.mousePosition;
        });
        _tab.RegisterCallback<MouseUpEvent>((x) => { OnCloseButtonClick(); });

        // Close Button
        _closeButton.clicked += () => { Destroy(gameObject); };

        // Messages to show up on starts
        if (ErrorsOnStart.Count > 0)
        {
            for (int i = 0; i < ErrorsOnStart.Count; i++)
            {
                AddError(ErrorsOnStart[i]);
            }
        }

        _root.Q<Button>("Validate").clicked += () => { OnValidateButtonClick(); };
    }

    private void Update()
    {
        if (_firstFrame)
        {
            _firstFrame = false;
            _windowPosition = Vector2.zero;
            _window.transform.position = _windowPosition;
        }

        if (_movementEngaged)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            mousePosition.y *= -1;
            mousePosition.y += Screen.height;
            _windowPosition = _mouseToWindowPosition + mousePosition;
            _window.transform.position = _windowPosition;
            if (!_mouseOverTab && Input.GetMouseButtonUp(0))
            {
                DisengageWindowMovement();
            }
        }
    }

    private void OnCloseButtonClick()
    {
        Destroy(gameObject);
    }

    private async void OnValidateButtonClick()
    {
        if (GameManager.Instance != null)
        {
            await GameManager.Instance.DisconnectAndUnloadWorlds();
            await LoadUtils.LoadSceneAsync("MainMenu", GameNetwork.SessionData.LoadingSteps.BackToMainMenu);
        }
    }

    private void DisengageWindowMovement()
    {
        _movementEngaged = false;

        // In case the window is outside the screen, put it back in
        _windowPosition.x = Mathf.Clamp(_windowPosition.x, -(Screen.width - _window.resolvedStyle.width) / 2, (Screen.width - _window.resolvedStyle.width) / 2);
        _windowPosition.y = Mathf.Clamp(_windowPosition.y, -(Screen.height - _window.resolvedStyle.height) / 2, (Screen.height - _tab.resolvedStyle.height * 2 + _window.resolvedStyle.height) / 2);

        _window.transform.position = _windowPosition;
    }

    public void AddError(string error)
    {
        Label label = new(error);
        _content.contentContainer.Add(label);
    }
}
