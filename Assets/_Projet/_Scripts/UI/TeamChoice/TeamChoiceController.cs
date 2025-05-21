using GameNetwork.Utils;
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
    private Button spectatorButton;

    private Label corpoNumberLabel;
    private Label natifNumberLabel;

    public UICentralController centralController { get => transform.parent.GetComponent<UICentralController>(); }

    private void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        corpoButton = root.Q<Button>("Corpo");
        natifButton = root.Q<Button>("Natif");
        spectatorButton = root.Q<Button>("Spectator");

        corpoNumberLabel = corpoButton.Q<Label>("TeamCount");
        natifNumberLabel = natifButton.Q<Label>("TeamCount");
    }

    private void Update()
    {
        int natifCount = PlayerHelpers.GetClientPlayersByTeam(TeamSideType.Natif).Count;
        int corpoCount = PlayerHelpers.GetClientPlayersByTeam(TeamSideType.Corpo).Count;

        corpoNumberLabel.text = $"{corpoCount} player(s)";
        natifNumberLabel.text = $"{natifCount} player(s)";
    }

    private void OnEnable()
    {
        corpoButton.RegisterCallback<ClickEvent>(OnCorpoButtonClicked);
        natifButton.RegisterCallback<ClickEvent>(OnNatifButtonClicked);
        spectatorButton.RegisterCallback<ClickEvent>(OnSpectatorButtonClicked);
    }

    private void OnDisable()
    {
        corpoButton.UnregisterCallback<ClickEvent>(OnCorpoButtonClicked);
        natifButton.UnregisterCallback<ClickEvent>(OnNatifButtonClicked);
        spectatorButton.UnregisterCallback<ClickEvent>(OnSpectatorButtonClicked);
    }

    private void OnCorpoButtonClicked(ClickEvent evt)
    {
        if (ClientTransportHelper.ClientWorld != null)
        {
            TeamChoiceSystemClient.SendTeamChoice(ClientTransportHelper.ClientWorld.EntityManager, TeamSideType.Corpo);
            centralController.SetUIActive(this, false);
            centralController.SetCursorActive(false);
            centralController.SetInputActive(true);
            centralController.SetUIActive(UICentralController.UIState.HUD, true);
            UI.SetActive(ref spectatorButton, false);
        }
    }

    private void OnNatifButtonClicked(ClickEvent evt)
    {

        if (ClientTransportHelper.ClientWorld != null)
        {
            TeamChoiceSystemClient.SendTeamChoice(ClientTransportHelper.ClientWorld.EntityManager, TeamSideType.Natif);
            centralController.SetUIActive(this, false);
            centralController.SetCursorActive(false);
            centralController.SetInputActive(true);
            centralController.SetUIActive(UICentralController.UIState.HUD, true);
            UI.SetActive(ref spectatorButton, false);
        }

    }

    private void OnSpectatorButtonClicked(ClickEvent evt)
    {
        centralController.SetUIActive(this, false);
        centralController.SetCursorActive(false);
        centralController.SetInputActive(true);
        centralController.SetUIActive(UICentralController.UIState.HUD, false);
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
