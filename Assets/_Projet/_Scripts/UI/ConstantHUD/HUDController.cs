using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
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
    HarvesterPlantingLinkSystem _harvesterPlantingSystem;
    HarvesterDefusingLinkSystem _harvesterDefusingSystem;

    // Bomb Interaction - Deffuse
    VisualElement _defuse;
    VisualElement _defuseFill;

    // Bomb Interaction - Plant
    VisualElement _plant;
    VisualElement _plantFill;

    // Round Information
    VisualElement _roundElement;
    Label _roundNumber;
    Label _roundPhase;
    VisualElement _roundBuyPhaseText;
    readonly float _roundPhaseTimer = 1f;
    float _roundPhaseTime = 0f;
    

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
        _roundNumber = _roundElement.Q<Label>("RoundNumber");
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
        UI.SetOpacity(ref _roundBuyPhaseText, 0f);
        UI.SetActive(ref _roundElement, false);
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
                _corpoScore.text = roundComponent.corporationScore.ToString().PadLeft(2, '0');
                _natifScore.text = roundComponent.nativeScore.ToString().PadLeft(2, '0');
                int seconds = Mathf.FloorToInt(roundComponent.timer) % 60;
                int minutes = Mathf.FloorToInt(roundComponent.timer) / 60;
                _second.text = seconds.ToString().PadLeft(2, '0');
                _minute.text = minutes.ToString().PadLeft(2, '0');
            }
        }

        // Open and close Message Box
        //if (Input.GetKeyDown(KeyCode.T))
        //{
        //    UI.ToggleActive(ref _messageBox);
        //}
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
