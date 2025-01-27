using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    VisualElement ui;

    Button playButton;
    Button quitButton;

    [SerializeField] SceneID sceneLoadOnPlay = SceneID.MultipayerTest;


    private void Awake()
    {
        UIDocument component;
        if (TryGetComponent<UIDocument>(out component))
        {
            ui = component.rootVisualElement;
        }
    }

    private void OnEnable()
    {
        if (ui != null)
        {
            playButton = ui.Q<Button>("PlayButton");
            playButton.clicked += OnPlayButton_Click;

            quitButton = ui.Q<Button>("QuitButton");
            quitButton.clicked += OnQuitButton_Click;
        }
    }

    void OnPlayButton_Click()
    {
        ConnectionManager.Instance.Connect();
        SceneManager.LoadScene((int)sceneLoadOnPlay);
    }

    void OnQuitButton_Click()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}
