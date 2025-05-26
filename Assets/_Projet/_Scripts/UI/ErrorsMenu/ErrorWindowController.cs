using GameNetwork.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ErrorWindowController : MonoBehaviour
{
    // Main Objects
    private UIDocument _document;
    private VisualElement _root;

    // Main Features
    private FloatingWindow _window;

    [HideInInspector] public List<string> ErrorsOnStart = new();

    private void Start()
    {
        // Initialize the Window elements
        _document = GetComponent<UIDocument>();
        _root = _document.rootVisualElement;

        _window = _root.Q<FloatingWindow>();

        // Messages to show up on starts
        if (ErrorsOnStart.Count > 0)
        {
            for (int i = 0; i < ErrorsOnStart.Count; i++)
            {
                AddError(ErrorsOnStart[i]);
            }
        }

        // Register Methods
        _window.AddCloseButtonMethod(OnCloseButtonClick);
        _window.AddValidateButtonMethod(OnValidateButtonClick);
    }

    private void OnCloseButtonClick()
    {
        Destroy(gameObject);
    }

    private async void OnValidateButtonClick()
    {
        await LoadUtils.LoadSceneAsync("MainMenu", GameNetwork.SessionData.LoadingSteps.BackToMainMenu);
    }

    public void AddError(string error)
    {
        Label errorMessage = UIDocumentUtils.Label(error, 20, Color.red);
        UIDocumentUtils.ToggleBold(ref errorMessage);
        _window.AddElement(errorMessage);
    }
}
