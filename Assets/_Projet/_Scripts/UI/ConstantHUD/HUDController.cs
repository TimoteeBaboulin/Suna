using GameNetwork.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;
using UI = UIDocumentUtils;

public class HUDController : MonoBehaviour
{
    // Main Objects
    private UIDocument _HUDDocument;
    private VisualElement _HUD;

    // Main HUD
    private VisualElement _crosshairElement;

    private Label _health;
    //private Label _armor; // Should be uncommented when Armor in working
    private Label _ammo;
    private Label _capacity;
    private Label _money;

    private Label _corpoScore;
    private Label _natifScore;

    private Label _minute;
    private Label _second;

    // Main Value Getter Systems
    private InGameHUDSystem _inGameHUDSystem = null;
    private bool _hitRegistered = false;
    private StyleColor _crosshairBaseColor;

    private RoundManagerLinkSystem _roundManagerLinkSystem = null;

    // Error Window
    private ErrorWindowCallerSystem _errorWindowCallerSystem = null;

    // Weapon List
    private VisualElement _weaponContainer;
    [SerializeField] private VisualTreeAsset _weaponAsset;
    [SerializeField] List<WeaponMap> _weaponMap;
    private WeaponListLinkSystem _weaponListLinkSystem;

    // Message Box // Should be uncommented when Message Box is fully implemented
    //private VisualElement _messageBox;
    //private ScrollView _messageBoxScrollView;

    // ErrorWindow
    [SerializeField] private GameObject _errorWindowPrefab;
    private GameObject _errorWindowInstance;

    // Bomb Interaction
    private HarvesterPlantingLinkSystem _harvesterPlantingSystem;
    private HarvesterDefusingLinkSystem _harvesterDefusingSystem;

    // Bomb Interaction - Deffuse
    private VisualElement _defuse;
    private VisualElement _defuseFill;

    // Bomb Interaction - Plant
    private VisualElement _plant;
    private VisualElement _plantFill;

    // Round Information
    private VisualElement _roundElement;
    private VisualElement _roundInfo;
    private Label _roundNumber;
    private Label _roundPhase;
    private VisualElement _roundBuyPhaseText;
    private readonly float _roundPhaseTimer = 4f;
    private float _roundPhaseTime = 0f;
    private RoundPhase _lastPhase = RoundPhase.PostRoundPhase;

    // Player Icons
    private VisualElement _corpoIcons;
    private VisualElement _natifIcons;


    private void Awake()
    {
        // Initialize all HUD elements
        _HUDDocument = GetComponent<UIDocument>();
        _HUD = _HUDDocument.rootVisualElement;

        _health = _HUD.Q<Label>("HealthLabel");
        //_armor = _HUD.Q<Label>("ArmorLabel");

        _ammo = _HUD.Q<Label>("AmmoLeftLabel");
        _capacity = _HUD.Q<Label>("AmmoCapacityLabel");

        _money = _HUD.Q<Label>("Cash");

        _crosshairElement = _HUD.Q("Crosshair");
        _crosshairBaseColor = _crosshairElement.style.unityBackgroundImageTintColor;

        _corpoScore = _HUD.Q<Label>("CorpoScore");
        _natifScore = _HUD.Q<Label>("NatifScore");

        _minute = _HUD.Q<VisualElement>("Timer").Q<Label>("Minute");
        _second = _HUD.Q<VisualElement>("Timer").Q<Label>("Second");

        _weaponContainer = _HUD.Q<VisualElement>("WeaponContainer");

        //_messageBox = _HUD.Q<VisualElement>("MessageBox");
        //_messageBoxScrollView = _messageBox.Q<ScrollView>();

        _defuse = _HUD.Q<VisualElement>("Defuse");
        _defuseFill = _defuse.Q<VisualElement>("DefuseFill");

        _plant = _HUD.Q<VisualElement>("Plant");
        _plantFill = _plant.Q<VisualElement>("PlantFill");

        _roundElement = _HUD.Q<VisualElement>("RoundPhaseElement");
        _roundInfo = _roundElement.Q<VisualElement>("RoundInfo");
        _roundNumber = _roundInfo.Q<Label>("RoundNumber");
        _roundPhase = _roundElement.Q<Label>("RoundPhaseLabel");
        _roundBuyPhaseText = _roundElement.Q<VisualElement>("RoundBuyText");
        _roundPhaseTime = _roundPhaseTimer;

        // Hide Message Box element at start
        //UI.SetActive(ref _messageBox, false);

        // Hide Defuse and Plant elements at start
        UI.SetActive(ref _defuse, false);
        UI.SetActive(ref _plant, false);

        // Hide Round Information elements at start
        UI.SetOpacity(ref _roundElement, 0f);
        UI.SetOpacity(ref _roundInfo, 0f);
        UI.SetOpacity(ref _roundBuyPhaseText, 0f);
        UI.SetActive(ref _roundElement, false);

        // Initialize Player Icons by hiding all of them
        _corpoIcons = _HUD.Q<VisualElement>("CorpoIcons");
        foreach (VisualElement icon in _corpoIcons.Children())
        {
            VisualElement iconRef = icon;
            UI.SetBorderColor(ref iconRef, Color.clear);
        }
        _natifIcons = _HUD.Q<VisualElement>("NatifIcons");
        foreach (VisualElement icon in _natifIcons.Children())
        {
            VisualElement iconRef = icon;
            UI.SetBorderColor(ref iconRef, Color.clear);
        }
    }

    private void Update()
    {
        // If too much message, delete previous ones
        //if (_messageBoxScrollView.contentContainer.childCount > 20) _messageBoxScrollView.contentContainer.RemoveAt(0);

        //----------System registering, all need to be in the right world
        if (_inGameHUDSystem == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            _inGameHUDSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InGameHUDSystem>();
            _inGameHUDSystem.HealthChangedEvent += System_OnHealthChange;
            _inGameHUDSystem.HitRegister += System_OnHitRegistered;
            _inGameHUDSystem.AmmoChangeEvent += System_OnAmmoChange;
            _inGameHUDSystem.MoneyChangedEvent += System_OnMoneyChange;
        }

        if (_roundManagerLinkSystem == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            _roundManagerLinkSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RoundManagerLinkSystem>();
        }

        if (_errorWindowCallerSystem == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            _errorWindowCallerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ErrorWindowCallerSystem>();
            _errorWindowCallerSystem.OnErrorMessageSent += OnErrorMessageReceived;
        }

        if (_harvesterPlantingSystem == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            _harvesterPlantingSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<HarvesterPlantingLinkSystem>();
            _harvesterPlantingSystem.OnPlantStart += OnPlantStarts;
            _harvesterPlantingSystem.OnPlantRunning += OnPlantRunning;
            _harvesterPlantingSystem.OnPlantCancelOrEnd += OnPlantCancelOrEnd;
        }

        if (_harvesterDefusingSystem == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            _harvesterDefusingSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<HarvesterDefusingLinkSystem>();
            _harvesterDefusingSystem.OnDefuseStart += OnDefuseStarts;
            _harvesterDefusingSystem.OnDefuseRunning += OnDefuseRunning;
            _harvesterDefusingSystem.OnDefuseCancelOrEnd += OnDefuseCancelOrEnd;
        }

        if (_weaponListLinkSystem == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            _weaponListLinkSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WeaponListLinkSystem>();
            _weaponListLinkSystem.OnStuffListChange += OnStuffListChange;
            _weaponListLinkSystem.OnStuffIdChange += OnStuffIdChange;
        }
        //---------- End of System Registering

        if (_hitRegistered)
        {
            _hitRegistered = false;
            StartCoroutine(HitRegistered());
        }

        // If RoundManager Linked, change values (for now in update and not when needed)
        if (_roundManagerLinkSystem != null)
        {
            if (_roundManagerLinkSystem.TryGetRoundComponent(out RoundComponent roundComponent))
            {
                RoundPhaseUpdate(roundComponent);
            }
        }

        // Open and close Message Box
        //if (Input.GetKeyDown(KeyCode.T))
        //{
        //    UI.ToggleActive(ref _messageBox);
        //}

        // Update Player Icons (should be updated only when it has to)
        List<IReadOnlyPlayer> teamList = PlayerHelpers.GetPlayersByTeam(TeamSideType.Corpo);
        for (int i = 0; i < _corpoIcons.Children().Count(); i++)
        {
            VisualElement icon = _corpoIcons.Q<VisualElement>("Position" + (i + 1).ToString());
            if (i < teamList.Count)
            {
                if (teamList[i].Id == ClientTransportHelper.instance.Session.CurrentPlayer.Id)
                {
                    UI.SetBorderColor(ref icon, Color.green);
                }
                else
                {
                    UI.SetBorderColor(ref icon, Color.gray);
                }
            }
            else
            {
                UI.SetBorderColor(ref icon, Color.clear);
            }
        }
        teamList.Clear();
        teamList = PlayerHelpers.GetPlayersByTeam(TeamSideType.Natif);
        for (int i = 0; i < _natifIcons.Children().Count(); i++)
        {
            VisualElement icon = _natifIcons.Q<VisualElement>("Position" + (i + 1).ToString());
            if (i < teamList.Count)
            {
                if (teamList[i].Id == ClientTransportHelper.instance.Session.CurrentPlayer.Id)
                {
                    UI.SetBorderColor(ref icon, Color.green);
                }
                else
                {
                    UI.SetBorderColor(ref icon, Color.gray);
                }
            }
            else
            {
                UI.SetBorderColor(ref icon, Color.clear);
            }
        }
    }

    private void RoundPhaseUpdate(RoundComponent roundComponent)
    {
        _corpoScore.text = roundComponent.corporationScore.ToString().PadLeft(2, '0');
        _natifScore.text = roundComponent.nativeScore.ToString().PadLeft(2, '0');
        int seconds = Mathf.FloorToInt(roundComponent.timer) % 60;
        int minutes = Mathf.FloorToInt(roundComponent.timer) / 60;
        _second.text = seconds.ToString().PadLeft(2, '0');
        _minute.text = minutes.ToString().PadLeft(2, '0');
        _roundNumber.text = roundComponent.currentRound.ToString();

        if (_lastPhase != roundComponent.currentPhase)
        {
            _lastPhase = roundComponent.currentPhase;
            _roundPhaseTime = 0f;
        }

        if (_roundPhaseTime < _roundPhaseTimer)
        {
            _roundPhaseTime += Time.deltaTime;

            if (_roundPhaseTime >= _roundPhaseTimer)
            {
                _roundPhaseTime = _roundPhaseTimer;
            }

            float t = _roundPhaseTime / _roundPhaseTimer;

            switch (_lastPhase)
            {
                case RoundPhase.BuyPhase:
                    _roundPhase.text = "BUY PHASE";
                    UpdateForBuyPhase(t);
                    break;
                case RoundPhase.ActionPhase:
                    _roundPhase.text = "ACTION PHASE";
                    UpdateForActionPhase(t);
                    break;
                case RoundPhase.PostPlantPhase:
                    _roundPhase.text = "HARVESTER PLANTED";
                    UpdateForPostPlantPhase(t);
                    break;
                case RoundPhase.PostRoundPhase:
                    _roundPhase.text = "END OF ROUND";
                    UpdateForPostRoundPhase(t);
                    break;
            }
        }
    }

    private void UpdateForBuyPhase(float t)
    {
        // Animation for Buy Phase
        // One forth of a second to pop up the visual
        // One half of a second to pop up Round Number and Buy Text

        UI.SetActive(ref _roundElement, true);

        if (t < .25f)
        {
            UI.SetOpacity(ref _roundElement, t * 4f);
        }
        else
        {
            UI.SetOpacity(ref _roundElement, 1f);
        }

        if (t < .5f)
        {
            UI.SetOpacity(ref _roundBuyPhaseText, t * 2f);
            UI.SetOpacity(ref _roundInfo, t * 2f);
        }
        else
        {
            UI.SetOpacity(ref _roundBuyPhaseText, 1f);
            UI.SetOpacity(ref _roundInfo, 1f);
        }
    }
    private void UpdateForActionPhase(float t)
    {
        // Animation for Action Phase
        // One forth of a second to fade away Round Number and Buy Text
        // One half of a second after a forth to fade away the visual

        UI.SetActive(ref _roundElement, true);

        if (t < .25f)
        {
            UI.SetOpacity(ref _roundBuyPhaseText, 1f - t * 4f);
            UI.SetOpacity(ref _roundInfo, 1f - t * 4f);
        }
        else
        {
            UI.SetOpacity(ref _roundBuyPhaseText, 0f);
            UI.SetOpacity(ref _roundInfo, 0f);
        }

        if (t < .25f)
        {
            UI.SetOpacity(ref _roundElement, 1f);
        }
        else if (t < .75f)
        {
            UI.SetOpacity(ref _roundElement, 1.5f - t * 2f);
        }
        else
        {
            UI.SetOpacity(ref _roundElement, 0f);
        }

        if (t == 1f)
        {
            UI.SetActive(ref _roundElement, false);
        }
    }
    private void UpdateForPostPlantPhase(float t)
    {
        // Animation for Action Phase
        // One forth of a second to pop up the visual
        // Last forth to fade away the visual

        UI.SetActive(ref _roundElement, true);

        if (t < .25f)
        {
            UI.SetOpacity(ref _roundElement, t * 4f);
        }
        else if (t < .75f)
        {
            UI.SetOpacity(ref _roundElement, 1f);
        }
        else if (t < 1f)
        {
            UI.SetOpacity(ref _roundElement, 4f - t * 4f);
        }
        else
        {
            UI.SetOpacity(ref _roundElement, 0f);
        }

        if (t == 1f)
        {
            UI.SetActive(ref _roundElement, false);
        }
    }
    private void UpdateForPostRoundPhase(float t)
    {
        // Animation for Action Phase
        // One forth of a second to pop up the visual
        // Last forth to fade away the visual

        UI.SetActive(ref _roundElement, true);

        if (t < .25f)
        {
            UI.SetOpacity(ref _roundElement, t * 4f);
        }
        else if (t < .75f)
        {
            UI.SetOpacity(ref _roundElement, 1f);
        }
        else if (t < 1f)
        {
            UI.SetOpacity(ref _roundElement, 4f - t * 4f);
        }
        else
        {
            UI.SetOpacity(ref _roundElement, 0f);
        }

        if (t == 1f)
        {
            UI.SetActive(ref _roundElement, false);
        }
    }

    //----------Start of Weapon List Link System
    private void OnStuffIdChange(object sender, WeaponListLinkSystem.StuffIdEventArgs args)
    {
        foreach (VisualElement weapon in _weaponContainer.Children())
        {
            bool selectedId = int.Parse(weapon.Q<Label>("Slot").text) == (args.StuffId + 1);
            weapon.style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, selectedId ? 1f : .125f);
        }
    }
    private void OnStuffListChange(object sender, WeaponListLinkSystem.StuffListChangeEventArgs args)
    {
        _weaponContainer.Clear();
        int count = 0;
        foreach (string stuffName in args.StuffListNames)
        {
            _weaponContainer.Add(_weaponAsset.Instantiate().Children().First());
            _weaponContainer.Children().Last().Q<Label>("Slot").text = (args.StuffListIds[count] + 1).ToString();
            _weaponContainer.Children().Last().style.backgroundImage = _weaponMap.Find(wm => wm.Weapon == stuffName).Tex;
            _weaponContainer.Children().Last().style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, .125f);
            count++;
        }
    }
    //----------End of Weapon List Link System

    //----------Start of Error Window System
    private void OnErrorMessageReceived(object sender, ErrorWindowCallerSystem.ErrorMessage args)
    {
        if (args.Messages.Count > 0)
        {
            if (_errorWindowInstance == null)
            {
                _errorWindowInstance = Instantiate(_errorWindowPrefab);
                ErrorWindowController errorWindowController = _errorWindowInstance.GetComponent<ErrorWindowController>();
                foreach (string message in args.Messages)
                {
                    errorWindowController.ErrorsOnStart.Add(message);
                }
            }
            else
            {
                ErrorWindowController errorWindowController = _errorWindowInstance.GetComponent<ErrorWindowController>();
                foreach (string message in args.Messages)
                {
                    errorWindowController.AddError(message);
                }
            }
        }
    }
    //----------End of Error Window System

    //----------Start of Main HUD Elements System
    IEnumerator HitRegistered()
    {
        yield return new WaitForSeconds(1f);
        _crosshairElement.style.unityBackgroundImageTintColor = _crosshairBaseColor;
        yield return null;
    }
    private void System_OnHealthChange(object sender, InGameHUDSystem.HealthArgs args)
    {
        _health.text = args.Health.ToString();
    }
    private void System_OnArmorChange(object sender, EventArgs args)
    {
        //armor.text = args.Armor.ToString();
    }
    private void System_OnAmmoChange(object sender, InGameHUDSystem.AmmoArgs args)
    {
        _ammo.text = args.ammo.ToString();
        _capacity.text = args.remainingAmmo.ToString();
    }
    private void System_OnMoneyChange(object sender, InGameHUDSystem.MoneyArgs args)
    {
        _money.text = args.money.ToString();
    }
    private void System_OnHitRegistered(object sender, EventArgs args)
    {
        _hitRegistered = true;
        _crosshairElement.style.unityBackgroundImageTintColor = new StyleColor(Color.red);
    }
    //----------End of Main HUD Elements System

    //----------Start of Message Box Element System
    //public void SendMessageToTchat(string message, Color messageColor)
    //{
    //    if (message == null) return;
    //    Label label = new(message);
    //    label.style.color = messageColor;
    //    label.style.fontSize = 20;
    //    _messageBoxScrollView.contentContainer.Add(label);
    //}
    //----------End of Message Box Element System

    //----------Start of Defuse and Plant Elements System
    public void SetActiveDefuse(bool value)
    {
        UI.SetActive(ref _defuse, value);
    }
    public void SetActivePlant(bool value)
    {
        UI.SetActive(ref _plant, value);
    }
    public void ResetDefuse()
    {
        UI.SetSize(ref _defuseFill, UI.PercentLength(0), UI.AutoLength());
    }
    public void ResetPlant()
    {
        UI.SetSize(ref _plantFill, UI.PercentLength(0), UI.AutoLength());
    }
    public void SetDefuseTime(float t)
    {
        UI.SetSize(ref _defuseFill, UI.PercentLength(t * 100f), UI.AutoLength());
    }
    public void SetPlantTime(float t)
    {
        UI.SetSize(ref _plantFill, UI.PercentLength(t * 100f), UI.AutoLength());
    }
    private void OnDefuseStarts(object sender, EventArgs args)
    {
        ResetDefuse();
        SetActiveDefuse(true);
    }
    private void OnDefuseRunning(object sender, HarvesterDefusingLinkSystem.HarversterDefuseRunning args)
    {
        SetDefuseTime(args.time / args.maxTime);
    }
    private void OnDefuseCancelOrEnd(object sender, EventArgs args)
    {
        SetActiveDefuse(false);
        ResetDefuse();
    }
    private void OnPlantStarts(object sender, EventArgs args)
    {
        ResetPlant();
        SetActivePlant(true);
    }
    private void OnPlantRunning(object sender, HarvesterPlantingLinkSystem.HarversterPlantRunning args)
    {
        SetPlantTime(args.time / args.maxTime);
    }
    private void OnPlantCancelOrEnd(object sender, EventArgs args)
    {
        SetActivePlant(false);
        ResetPlant();
    }
    //----------End of Defuse and Plant Elements System
}

[Serializable]
public struct WeaponMap
{
    public string Weapon;
    public Texture2D Tex;
}
