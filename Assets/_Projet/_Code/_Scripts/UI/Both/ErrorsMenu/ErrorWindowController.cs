using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ErrorWindowController : MonoBehaviour
{
    private UIDocument _document;
    private VisualElement _root;

    private VisualElement _window;
    private VisualElement _tab;
    private ScrollView _content;
    private Button _closeButton;

    bool mouseOverTab = false;
    Vector2 windowPosition;
    bool movementEngaged = false;
    Vector2 mouseToWindowPosition = Vector2.zero;

    bool firstFrame = true;
    int count = 0;

    [HideInInspector] public List<string> errorsOnStart = new();

    private void Start()
    {
        _document = GetComponent<UIDocument>();
        _root = _document.rootVisualElement;

        _window = _root.Children().First();
        _tab = _root.Q("Tab");
        _content = _root.Q<ScrollView>();
        _closeButton = _root.Q<Button>("Close");

        _tab.RegisterCallback<MouseEnterEvent>((x) => { mouseOverTab = true; });
        _tab.RegisterCallback<MouseLeaveEvent>((x) => { mouseOverTab = false; });
        _tab.RegisterCallback<MouseDownEvent>((x) =>
        {
            movementEngaged = true;
            mouseToWindowPosition = windowPosition - x.mousePosition;
        });
        _tab.RegisterCallback<MouseUpEvent>((x) => { DisengageWindowMovement(); });

        _closeButton.clicked += () => { Destroy(gameObject); };

        if (errorsOnStart.Count > 0)
        {
            for (int i = 0; i < errorsOnStart.Count; i++)
            {
                AddError(errorsOnStart[i]);
            }
        }

        _root.Q<Button>("Validate").clicked += () => { SceneManager.LoadScene(0); };
    }

    private void Update()
    {
        if (firstFrame)
        {
            firstFrame = false;
            windowPosition = Vector2.zero;
            _window.transform.position = windowPosition;
        }

        if (movementEngaged)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            mousePosition.y *= -1;
            mousePosition.y += Screen.height;
            windowPosition = mouseToWindowPosition + mousePosition;
            _window.transform.position = windowPosition;
            if (!mouseOverTab && Input.GetMouseButtonUp(0))
            {
                DisengageWindowMovement();
            }
        }
    }

    private void DisengageWindowMovement()
    {
        movementEngaged = false;
        if (windowPosition.x < -(Screen.width - _window.resolvedStyle.width) / 2)
            windowPosition.x = -(Screen.width - _window.resolvedStyle.width) / 2;
        if (windowPosition.y < -(Screen.height - _window.resolvedStyle.height) / 2)
            windowPosition.y = -(Screen.height - _window.resolvedStyle.height) / 2;
        if (windowPosition.x > (Screen.width - _window.resolvedStyle.width) / 2)
            windowPosition.x = (Screen.width - _window.resolvedStyle.width) / 2;
        if (windowPosition.y > (Screen.height - _tab.resolvedStyle.height * 2 + _window.resolvedStyle.height) / 2)
            windowPosition.y = (Screen.height - _tab.resolvedStyle.height * 2 + _window.resolvedStyle.height) / 2;
        _window.transform.position = windowPosition;
    }

    public void AddError(string error)
    {
        Label label = new(error);
        _content.contentContainer.Add(label);
    }
}
