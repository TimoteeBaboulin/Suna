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
        ui = GetComponent<UIDocument>().rootVisualElement;
    }

    private void OnEnable()
    {
        playButton = ui.Q<Button>("PlayButton");
        playButton.clicked += OnPlayButton_Click;

        quitButton = ui.Q<Button>("QuitButton");
        quitButton.clicked += OnQuitButton_Click;
    }

    void OnPlayButton_Click()
    {
        SceneLoader.Instance.ChangeScene(sceneLoadOnPlay);
    }

    void OnQuitButton_Click()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}
