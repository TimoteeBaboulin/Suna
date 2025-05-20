using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

using UI = UIDocumentUtils;

public class TeamChoiceController : MonoBehaviour
{
    private VisualElement root;
    private Button corpoButton;
    private Button natifButton;

    private void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        corpoButton = root.Q<Button>("Corpo");
        natifButton = root.Q<Button>("Natif");
    }

    private void Update()
    {
        if (Keyboard.current.commaKey.wasPressedThisFrame)
        {
            SetUIActive(!UI.IsActive(ref root));
        }
    }

    private void OnEnable()
    {
        corpoButton.RegisterCallback<ClickEvent>(OnCorpoButtonClicked);
        natifButton.RegisterCallback<ClickEvent>(OnNatifButtonClicked);
    }

    private void OnDisable()
    {
        corpoButton.UnregisterCallback<ClickEvent>(OnCorpoButtonClicked);
        natifButton.UnregisterCallback<ClickEvent>(OnNatifButtonClicked);
    }

    private void OnCorpoButtonClicked(ClickEvent evt)
    {
        TeamChoiceSystemClient.SendTeamChoice(World.DefaultGameObjectInjectionWorld.EntityManager, TeamSideType.Corpo);
        SetUIActive(false);
    }

    private void OnNatifButtonClicked(ClickEvent evt)
    {
        TeamChoiceSystemClient.SendTeamChoice(World.DefaultGameObjectInjectionWorld.EntityManager, TeamSideType.Natif);
        SetUIActive(false);
    }

    private void SetUIActive(bool value)
    {
        UI.SetActive(ref root, value);
        if (value)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }
}
