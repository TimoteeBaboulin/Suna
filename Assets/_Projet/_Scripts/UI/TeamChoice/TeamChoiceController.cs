using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

using UI = UIDocumentUtils;

public class TeamChoiceController : MonoBehaviour, IUIController
{
    private VisualElement root;
    private Button corpoButton;
    private Button natifButton;

    public UICentralController centralController { get => transform.parent.GetComponent<UICentralController>(); }

    private void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        corpoButton = root.Q<Button>("Corpo");
        natifButton = root.Q<Button>("Natif");
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
        centralController.SetUIActive(this, false);
        centralController.SetCursorActive(false);
        centralController.SetInputActive(true);
    }

    private void OnNatifButtonClicked(ClickEvent evt)
    {
        TeamChoiceSystemClient.SendTeamChoice(World.DefaultGameObjectInjectionWorld.EntityManager, TeamSideType.Natif);
        centralController.SetUIActive(this, false);
        centralController.SetCursorActive(false);
        centralController.SetInputActive(true);
    }

    public void SetUIActive(bool value)
    {
        UI.SetActive(ref root, value);
    }

    public bool IsUIActive()
    {
        return UI.IsActive(ref root);
    }

    public UICentralController.UIState GetUIState()
    {
        return UICentralController.UIState.TEAM_CHOICE;
    }
}
