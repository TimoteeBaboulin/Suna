using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IUIController
{
    public UICentralController centralController { get; }
    public void SetUIActive(bool value);
    public void ToggleUIActive()
    {
        if (IsUIActive())
        {
            SetUIActive(false);
        }
        else
        {
            SetUIActive(true);
        }
    }
    public bool IsUIActive();

    public UICentralController.UIState GetUIState();
}

public class UICentralController : MonoBehaviour
{
    [SerializeField] private HUDController _hudController;
    [SerializeField] private TeamChoiceController _teamChoiceController;
    [SerializeField] private ShopController _shopController;
    [SerializeField] private PauseMenuController _pauseMenuController;

    private DefaultInputSystem input;
    bool inputFound = false;

    public enum UIState
    {
        NO_STATE = 1 << 0,
        HUD = 1 << 1,
        TEAM_CHOICE = 1 << 2,
        SHOP = 1 << 3,
        PAUSE_MENU = 1 << 4
    }
    private int _uiState = (int)UIState.NO_STATE;

    private void Start()
    {
        _uiState |= (int)UIState.HUD; // Initialize with HUD active
        _uiState |= (int)UIState.TEAM_CHOICE; // Initialize with Team Choice active

        SetUIActive(_hudController, true);
        SetUIActive(_teamChoiceController, true);
        SetUIActive(_shopController, false);
        SetUIActive(_pauseMenuController, false);
    }

    private void Update()
    {
        if (_hudController == null || _teamChoiceController == null || _shopController == null || _pauseMenuController == null)
        {
            Debug.LogError("One or more UI controllers are not assigned in the inspector.");
            if (_hudController != null) { _hudController.SetUIActive(false); }
            if (_teamChoiceController != null) { _teamChoiceController.SetUIActive(false); }
            if (_shopController != null) { _shopController.SetUIActive(false); }
            if (_pauseMenuController != null) { _pauseMenuController.SetUIActive(false); }
            return;
        }

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) { return; }
        CharacterInputSystem system = world.GetExistingSystemManaged<CharacterInputSystem>();

        if (system != null)
        {
            input = system.input;
            inputFound = true;
        }
        else
        {
            inputFound = false;
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (IsUIStateActive(UIState.SHOP)) // First if Shop is open, close it
            {
                SetUIActive(_shopController, false);
                SetCursorActive(false);
            }
            else if (IsUIStateActive(UIState.TEAM_CHOICE)) // If not but Team Choice is open, close it
            {
                SetUIActive(_teamChoiceController, false);
                SetCursorActive(false);
            }
            else // If nothing was open, toggle the Pause Menu
            {
                ToggleUIActive(_pauseMenuController);
                SetCursorActive(IsUIActive(_pauseMenuController)); // Set cursor active if Pause Menu is open, otherwise set it inactive
                SetInputActive(!IsUIActive(_pauseMenuController)); // Disable input if Pause Menu is open, otherwise enable it
            }
        }
        else if (keyboard.bKey.wasPressedThisFrame && _hudController.GetCurrentPlayerInfo().team != TeamSideType.Neutre)
        {
            if (!IsUIStateActive(UIState.PAUSE_MENU) && !IsUIStateActive(UIState.TEAM_CHOICE)) // If Pause Menu or Team Choice is not open
            {
                var query = world.EntityManager.CreateEntityQuery(typeof(RoundComponent));
                var entityArray = query.ToEntityArray(Allocator.Temp);
                if (entityArray.Length == 0 || entityArray.Length > 1) return; // Check if RoundComponent exists or if there are multiple instances
                RoundComponent roundData = world.EntityManager.GetComponentData<RoundComponent>(entityArray[0]);
                if (roundData.currentPhase == RoundPhase.BuyPhase || !roundData.roundSystemActive)
                {
                    ToggleUIActive(_shopController);
                    SetCursorActive(IsUIActive(_shopController)); // Set cursor active if Shop is open, otherwise set it inactive
                    SetInputActive(!IsUIActive(_shopController)); // Disable input if Shop is open, otherwise enable it
                }
            }
        }
        else if (keyboard.commaKey.wasPressedThisFrame)
        {
            if (!IsUIStateActive(UIState.PAUSE_MENU) && !IsUIStateActive(UIState.SHOP)) // If Pause Menu or Shop is not open
            {
                ToggleUIActive(_teamChoiceController);
                SetCursorActive(IsUIActive(_teamChoiceController)); // Set cursor active if Team Choice is open, otherwise set it inactive
                SetInputActive(!IsUIActive(_teamChoiceController)); // Disable input if Team Choice is open, otherwise enable it
            }
        }
    }

    public void SetUIActive(IUIController uiController, bool value)
    {
        uiController.SetUIActive(value);
        _uiState = value ? _uiState | (int)uiController.GetUIState() : _uiState & ~(int)uiController.GetUIState();
    }

    public void SetUIActive(UIState state, bool value)
    {
        switch (state)
        {
            case UIState.HUD:
                SetUIActive(_hudController, value);
                break;
            case UIState.TEAM_CHOICE:
                SetUIActive(_teamChoiceController, value);
                break;
            case UIState.SHOP:
                SetUIActive(_shopController, value);
                break;
            case UIState.PAUSE_MENU:
                SetUIActive(_pauseMenuController, value);
                break;
            default:
                Debug.LogError("Invalid UI state: " + state);
                break;
        }
    }

    public void ToggleUIActive(IUIController uiController)
    {
        uiController.ToggleUIActive();
        _uiState ^= (int)uiController.GetUIState();
    }

    public bool IsUIActive(IUIController uiController)
    {
        return uiController.IsUIActive();
    }

    public void SetCursorActive(bool value)
    {
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

    public void SetInputActive(bool value)
    {
        if (value)
        {
            input.Player.Enable();
        }
        else
        {
            input.Player.Disable();
        }
    }

    public bool IsUIStateActive(UIState state)
    {
        return (_uiState & (int)state) != 0;
    }
}
