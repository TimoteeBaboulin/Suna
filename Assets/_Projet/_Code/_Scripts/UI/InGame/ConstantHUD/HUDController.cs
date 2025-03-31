using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    // Main Features
    private UIDocument _HUDDocument;

    private VisualElement _HUD;

    private VisualElement _crosshairElement;

    private Label _health;
    private Label _armor;
    private Label _ammo;
    private Label _capacity;
    private Label _money;

    private Label _corpoScore;
    private Label _natifScore;

    private Label _minute;
    private Label _second;

    private bool _hitRegistered = false;

    private InGameHUDSystem _inGameHUDSystem = null;
    private RoundManagerLinkSystem _roundManagerLinkSystem = null;
    private ErrorWindowCallerSystem _errorWindowCallerSystem = null;

    private StyleColor _crosshairBaseColor;

    // Weapon Slot
    private VisualElement _weaponContainer;
    [SerializeField] private VisualTreeAsset _weaponAsset;
    [SerializeField] List<WeaponMap> _weaponMap;
    [SerializeField] List<WeaponSlot> _weaponSlot;
    private int selectedSlot = 0;

    // Message Box
    private VisualElement _messageBox;
    private ScrollView _messageBoxScrollView;

    // ErrorWindow
    [SerializeField] private GameObject _errorWindowPrefab;
    private GameObject _errorWindowInstance;

    private void Awake()
    {
        // Initialize all HUD elements
        _HUDDocument = GetComponent<UIDocument>();
        _HUD = _HUDDocument.rootVisualElement;

        _health = _HUD.Q<Label>("HealthLabel");
        _armor = _HUD.Q<Label>("ArmorLabel");

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

        _messageBox = _HUD.Q<VisualElement>("MessageBox");
        _messageBoxScrollView = _messageBox.Q<ScrollView>();

        // Initialize Weapon Container
        for (int i = 0; i < _weaponSlot.Count; i++)
        {
            _weaponContainer.Add(_weaponAsset.Instantiate().Children().First());
            _weaponContainer.Children().Last().Q<Label>("Slot").text = _weaponSlot[i].SlotNumber.ToString();
            _weaponContainer.Children().Last().style.backgroundImage = new()
            {
                value = new()
                {
                    texture = _weaponMap.Find(wm => wm.Weapon == _weaponSlot[i].Weapon).Tex
                }
            };
            _weaponContainer.Children().Last().style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, i == selectedSlot ? 1f : .125f);
        }

        // Hide Message Box at start
        _messageBox.style.opacity = 0;
        _messageBox.SetEnabled(false);
    }

    private void Update()
    {
        // If too much message, delete previous ones
        if (_messageBoxScrollView.contentContainer.childCount > 20) _messageBoxScrollView.contentContainer.RemoveAt(0);

        // Initialize InGameHUDSystem in Update because need to be in the right world
        if (_inGameHUDSystem == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            _inGameHUDSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InGameHUDSystem>();
            _inGameHUDSystem.HealthChangedEvent += System_OnHealthChange;
            _inGameHUDSystem.HitRegister += System_OnHitRegistered;
            _inGameHUDSystem.AmmoChangeEvent += System_OnAmmoChange;
            _inGameHUDSystem.MoneyChangedEvent += System_OnMoneyChange;
        }

        // Initialize RoundManagerLinkSystem in Update because need to be in the right world
        if (_roundManagerLinkSystem == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            _roundManagerLinkSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RoundManagerLinkSystem>();
        }

        if (_errorWindowCallerSystem == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            _errorWindowCallerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ErrorWindowCallerSystem>();
            _errorWindowCallerSystem.OnErrorMessageSent += OnErrorMessageReceived;
        }

        if (_hitRegistered)
        {
            _hitRegistered = false;
            StartCoroutine(HitRegistered());
        }

        // Weapon Selection Scrolling
        if (Input.GetAxis("Mouse ScrollWheel") < 0) // backward
        {
            _weaponContainer.Children().ToList()[selectedSlot].style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, .125f);
            selectedSlot = (selectedSlot + 1) % _weaponSlot.Count;
            _weaponContainer.Children().ToList()[selectedSlot].style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, 1f);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") > 0) // forward
        {
            _weaponContainer.Children().ToList()[selectedSlot].style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, .125f);
            if (selectedSlot == 0) selectedSlot = _weaponSlot.Count - 1;
            else selectedSlot = (selectedSlot - 1) % _weaponSlot.Count;
            _weaponContainer.Children().ToList()[selectedSlot].style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, 1f);
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
        if (Input.GetKeyDown(KeyCode.T))
        {
            _messageBox.style.opacity = _messageBox.style.opacity.value == 1 ? 0 : 1;
            _messageBox.SetEnabled(_messageBox.enabledInHierarchy);
        }
    }

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
    private void System_OnArmorChange(object sender, TestPlayerDataSystem.ArmorArgs args)
    {
        //if (args is TestPlayerDataSystem.ArmorArgs arg) armor.text = arg.Armor.ToString();
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
    private void System_OnCapacityChange(object sender, TestPlayerDataSystem.CapacityArgs args)
    {
        //if (args is TestPlayerDataSystem.CapacityArgs arg) capacity.text = arg.Capacity.ToString();
    }

    private void System_OnHitRegistered(object sender, EventArgs args)
    {
        _hitRegistered = true;
        _crosshairElement.style.unityBackgroundImageTintColor = new StyleColor(Color.red);
    }

    public void SendMessageToTchat(string message, Color messageColor)
    {
        if (message == null) return;
        Label label = new(message);
        label.style.color = messageColor;
        label.style.fontSize = 20;
        _messageBoxScrollView.contentContainer.Add(label);
    }
}

[Serializable]
public struct WeaponSlot
{
    public int SlotNumber;
    public string Weapon;
}

[Serializable]
public struct WeaponMap
{
    public string Weapon;
    public Texture2D Tex;
}
