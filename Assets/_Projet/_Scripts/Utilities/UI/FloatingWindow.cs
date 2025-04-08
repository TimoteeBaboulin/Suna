using System;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[UxmlElement]
public partial class FloatingWindow : SafeVisualElement
{
    private Label _titleLabel;
    private string _titleString = "Title";
    private VisualElement _tabElement;
    private Button _closeButton;
    private ScrollView _scrollView;
    private Button _validateButton;
    private string _validateString = "Validate";

    private bool _mouseOverTab = false;
    private Vector2 _windowPosition;
    private bool _movementEngaged = false;
    private Vector2 _mouseToWindowPosition = Vector2.zero;

    [UxmlAttribute]
    public string TitleString
    {
        get => _titleString;
        set
        {
            _titleString = value;
            if (_titleLabel != null)
                _titleLabel.text = _titleString;
        }
    }

    [UxmlAttribute]
    public string ValidateString
    {
        get => _validateString;
        set
        {
            _validateString = value;
            if (_validateButton != null)
                _validateButton.text = _validateString;
        }
    }

    protected override void OnInit()
    {
        StyleSheet floatingWindowStyleSheet = Addressables.LoadAssetAsync<StyleSheet>("floating-window").WaitForCompletion();
        if (floatingWindowStyleSheet != null)
        {
            styleSheets.Add(floatingWindowStyleSheet);
        }
        else
        {
            Debug.LogError("FloatingWindow StyleSheet not found.");
        }
        // Set up the floating window
        AddToClassList("floating-window");

        // Set up the Tab
        _tabElement = new()
        {
            name = "floating-window-tab"
        };
        _tabElement.AddToClassList(_tabElement.name);
        Add(_tabElement);

        // Set up the Title Label
        _titleLabel = new()
        {
            name = "floating-window-title"
        };
        _titleLabel.AddToClassList(_titleLabel.name);
        _tabElement.Add(_titleLabel);
        _titleLabel.text = _titleString;

        // Set up the Close Button
        _closeButton = new()
        {
            name = "floating-window-close-button"
        };
        _closeButton.AddToClassList(_closeButton.name);
        _tabElement.Add(_closeButton);
        _closeButton.text = "X";

        // Set up the ScrollView
        _scrollView = new()
        {
            name = "floating-window-scrollview"
        };
        _scrollView.AddToClassList(_scrollView.name);
        Add(_scrollView);

        // Set up the Validate Button
        _validateButton = new()
        {
            name = "floating-window-validate-button"
        };
        _validateButton.AddToClassList(_validateButton.name);
        Add(_validateButton);
        _validateButton.text = _validateString;

        // Set up Callbacks and Clicks
        _tabElement.RegisterCallback<MouseEnterEvent>(OnMouseEnterTab);
        _tabElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveTab);
        _tabElement.RegisterCallback<MouseDownEvent>(OnMouseDownTab);
        _tabElement.RegisterCallback<MouseUpEvent>(OnMouseUpTab);

        schedule.Execute(Update).Every(16);
    }

    private void Update()
    {
        if (_movementEngaged)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            mousePosition.y *= -1;
            mousePosition.y += Screen.height;
            _windowPosition = _mouseToWindowPosition + mousePosition;
            style.left = _windowPosition.x;
            style.top = _windowPosition.y;
            if (!_mouseOverTab && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                DisengageWindowMovement();
            }
        }
    }

    private void OnMouseEnterTab(MouseEnterEvent evt)
    {
        _mouseOverTab = true;
    }
    private void OnMouseLeaveTab(MouseLeaveEvent evt)
    {
        _mouseOverTab = false;
    }
    private void OnMouseDownTab(MouseDownEvent evt)
    {
        _movementEngaged = true;
        _mouseToWindowPosition = new Vector2(resolvedStyle.left, resolvedStyle.top) - evt.mousePosition;
    }
    private void OnMouseUpTab(MouseUpEvent evt)
    {
        DisengageWindowMovement();
    }
    private void DisengageWindowMovement()
    {
        _movementEngaged = false;
        
        _windowPosition.x = Mathf.Clamp(_windowPosition.x, 0f, (parent != null ? parent.resolvedStyle.width : Screen.width) - resolvedStyle.width);
        _windowPosition.y = Mathf.Clamp(_windowPosition.y, 0f, (parent != null ? parent.resolvedStyle.height : Screen.height) - _tabElement.resolvedStyle.height);

        style.left = _windowPosition.x;
        style.top = _windowPosition.y;
    }

    public void AddElement(VisualElement element)
    {
        _scrollView.Add(element);
    }

    public void AddCloseButtonMethod(Action method) => _closeButton.clicked += method;
    public void RemoveCloseButtonMethod(Action method) => _closeButton.clicked -= method;
    public void AddValidateButtonMethod(Action method) => _validateButton.clicked += method;
    public void RemoveValidateButtonMethod(Action method) => _validateButton.clicked -= method;
}
