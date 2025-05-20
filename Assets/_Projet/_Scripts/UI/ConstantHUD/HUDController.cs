using GameNetwork.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;
using UI = UIDocumentUtils;

public class HUDController : MonoBehaviour, IUIController
{
    private UITextureData texData;

    // Main Objects
    private UIDocument _HUDDocument;
    private VisualElement _HUD;

    // Main HUD
    private bool _damageIndicatorSubscribed = false;
    private VisualElement _crosshairElement;
    private VisualElement _hitMarkerElement;
    private VisualElement _sniperElement;

    //Grenade effects
    private VisualElement _flash;
    private VisualElement _smokeEffect;
    private VisualElement _gasEffect;

    private Label _health;
    private Label _armor;
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
    private float _hitMarkerTimer = 0f;
    readonly private float _hitMarkerTime = 0.3f;

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

    // KillFeed
    private VisualElement _killFeedContainer;
    private bool _killFeedSubscribed = false;

    // WinLose
    private VisualElement _winLoseRoundElement;
    private VisualElement _winLoseGameElement;
    readonly private float _winLoseGameTime = 1.5f;
    private float _winLoseGameTimer = 0f;
    readonly private float _winLoseEndGameTime = 5f;
    private bool _winLoseRoundSubscribed = false;
    private bool _winLoseRoundEngaged = false;
    private float _winLoseRoundTimer = 0f;
    readonly private float _winLoseRoundTime = 4f;

    // Minimap
    private bool _minimapSubscribed = false;
    private VisualElement _minimapElement;
    private VisualElement _minimapMapElement;
    private VisualElement _minimapPlayerElement;

    // Damage Overlay
    private VisualElement _damageOverlay;
    private bool _damageOverlayActive = false;
    private float _damageOverlayTimer = 0f;
    private readonly float _damageOverlayTime = 0.5f;

    public UICentralController centralController { get => GetComponentInParent<UICentralController>(); }

    private void Awake()
    {
        // Initialize all HUD elements
        _HUDDocument = GetComponent<UIDocument>();
        _HUD = _HUDDocument.rootVisualElement;

        _flash = _HUD.Q<VisualElement>("Flash");
        _smokeEffect = _HUD.Q<VisualElement>("SmokeHUD");
        _gasEffect = _HUD.Q<VisualElement>("GasHUD");
        _sniperElement = _HUD.Q<VisualElement>("SniperCrosshair");

        _health = _HUD.Q<Label>("HealthLabel");
        _armor = _HUD.Q<Label>("ArmorLabel");

        _ammo = _HUD.Q<Label>("AmmoLeftLabel");
        _capacity = _HUD.Q<Label>("AmmoCapacityLabel");

        _money = _HUD.Q<Label>("Cash");

        _crosshairElement = _HUD.Q("Crosshair");
        _hitMarkerElement = _crosshairElement.Q("HitMarker");
        UI.SetOpacity(ref _hitMarkerElement, 0f);

        _damageOverlay = _HUD.Q("DamageOverlay");
        UI.SetOpacity(ref _damageOverlay, 0f);

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

        _minimapElement = _HUD.Q<VisualElement>("Minimap");
        _minimapMapElement = _minimapElement.Q<VisualElement>("Map");
        _minimapPlayerElement = _minimapElement.Q<VisualElement>("Player");

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
            UI.SetImageTintColor(ref iconRef, Color.clear);
        }
        _natifIcons = _HUD.Q<VisualElement>("NatifIcons");
        foreach (VisualElement icon in _natifIcons.Children())
        {
            VisualElement iconRef = icon;
            UI.SetBorderColor(ref iconRef, Color.clear);
            UI.SetImageTintColor(ref iconRef, Color.clear);
        }

        // Initialize KillFeed
        _killFeedContainer = _HUD.Q<VisualElement>("KillFeedContainer");

        // WinLose
        _winLoseRoundElement = _HUD.Q<VisualElement>("WinLoseRound");
        _winLoseGameElement = _HUD.Q<VisualElement>("WinLoseGame");
        UI.SetActive(ref _winLoseRoundElement, false);
        UI.SetActive(ref _winLoseGameElement, false);
        UI.SetOpacity(ref _winLoseRoundElement, 0f);
        UI.SetOpacity(ref _winLoseGameElement, 0f);
    }

    private void Start()
    {
        texData = Addressables.LoadAssetAsync<UITextureData>("UITextureData").WaitForCompletion();
    }

    private void Update()
    {
        foreach (VisualElement playerPin in _minimapMapElement.Children())
        {
            VisualElement visualElement = playerPin;
            UI.SetActive(ref visualElement, false);
            UI.SetOpacity(ref visualElement, 0f);
        }

        // If too much message, delete previous ones
        //if (_messageBoxScrollView.contentContainer.childCount > 20) _messageBoxScrollView.contentContainer.RemoveAt(0);

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
            return;
        //----------System registering, all need to be in the right world
        if (_inGameHUDSystem == null && world.Name == "ClientWorld")
        {
            _inGameHUDSystem = world.GetExistingSystemManaged<InGameHUDSystem>();
            _inGameHUDSystem.HealthChangedEvent += System_OnHealthChange;
            _inGameHUDSystem.HitRegister += System_OnHitRegistered;
            _inGameHUDSystem.AmmoChangeEvent += System_OnAmmoChange;
            _inGameHUDSystem.MoneyChangedEvent += System_OnMoneyChange;
            _inGameHUDSystem.FlashGrenadeEvent += System_OnFlashGrenade;
            _inGameHUDSystem.PositionChangedEvent += System_OnPositionChanged;
            _inGameHUDSystem.ADSChangedEvent += System_OnADSChange;
            _inGameHUDSystem.SmokeGrenadeEvent += System_OnSmokeGrenade;
        }

        if (_roundManagerLinkSystem == null && world.Name == "ClientWorld")
        {
            _roundManagerLinkSystem = world.GetExistingSystemManaged<RoundManagerLinkSystem>();
        }

        if (_errorWindowCallerSystem == null && world.Name == "ClientWorld")
        {
            _errorWindowCallerSystem = world.GetExistingSystemManaged<ErrorWindowCallerSystem>();
            _errorWindowCallerSystem.OnErrorMessageSent += OnErrorMessageReceived;
        }

        if (_harvesterPlantingSystem == null && world.Name == "ClientWorld")
        {
            _harvesterPlantingSystem = world.GetExistingSystemManaged<HarvesterPlantingLinkSystem>();
            _harvesterPlantingSystem.OnPlantStart += OnPlantStarts;
            _harvesterPlantingSystem.OnPlantRunning += OnPlantRunning;
            _harvesterPlantingSystem.OnPlantCancelOrEnd += OnPlantCancelOrEnd;
        }

        if (_harvesterDefusingSystem == null && world.Name == "ClientWorld")
        {
            _harvesterDefusingSystem = world.GetExistingSystemManaged<HarvesterDefusingLinkSystem>();
            _harvesterDefusingSystem.OnDefuseStart += OnDefuseStarts;
            _harvesterDefusingSystem.OnDefuseRunning += OnDefuseRunning;
            _harvesterDefusingSystem.OnDefuseCancelOrEnd += OnDefuseCancelOrEnd;
        }

        if (_weaponListLinkSystem == null && world.Name == "ClientWorld")
        {
            _weaponListLinkSystem = world.GetExistingSystemManaged<WeaponListLinkSystem>();
            _weaponListLinkSystem.OnStuffListChange += OnStuffListChange;
            _weaponListLinkSystem.OnStuffIdChange += OnStuffIdChange;
        }

        if (!_winLoseRoundSubscribed && world.Name == "ClientWorld")
        {
            RoundSystemClient.OnTeamWinRound += OnTeamWinRound;
            _winLoseRoundSubscribed = true;
        }
        if (!_minimapSubscribed && world.Name == "ClientWorld")
        {
            MinimapTeamLinkSystem.OnMinimapTeamLinkEvent += System_OnPositionChanged;
            _minimapSubscribed = true;
        }
        if (!_damageIndicatorSubscribed && world.Name == "ClientWorld")
        {
            DamageSourcePositionSystem.OnDamageIndicatorReceived += System_OnDamageTaken;
            _damageIndicatorSubscribed = true;
        }
        if (!_killFeedSubscribed && world.Name == "ClientWorld")
        {
            KillFeedRPCSystem.OnKillDamageIndicatorReceived += InitializeNewKillFeed;
            _killFeedSubscribed = true;
        }
        //---------- End of System Registering

        if (_hitRegistered)
        {
            _hitMarkerTimer -= Time.deltaTime;
            if (_hitMarkerTimer < 0f)
            {
                _hitMarkerTimer = 0f;
            }

            float t = Mathf.Clamp01(_hitMarkerTimer / _hitMarkerTime);

            UI.SetOpacity(ref _hitMarkerElement, t);

            if (_hitMarkerTimer <= 0f)
            {
                _hitMarkerTimer = 0f;
                _hitRegistered = false;
                _hitMarkerElement.style.backgroundImage = null;
                UI.SetOpacity(ref _hitMarkerElement, 0f);
            }
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

        if (world.Name == "ClientWorld")
        {
            PlayerIconsUpdate(TeamSideType.Corpo, _corpoIcons);
            PlayerIconsUpdate(TeamSideType.Natif, _natifIcons);
        }

        if (world.Name == "ClientWorld" && _winLoseRoundEngaged)
        {
            OnWinLoseRoundUpdate();
        }

        if (_damageOverlayActive)
        {
            _damageOverlayTimer -= Time.deltaTime;
            if (_damageOverlayTimer < 0f)
            {
                _damageOverlayTimer = 0f;
            }

            float t = Mathf.Clamp01(_damageOverlayTimer / _damageOverlayTime);

            UI.SetOpacity(ref _damageOverlay, t);

            if (_damageOverlayTimer <= 0f)
            {
                _damageOverlayTimer = 0f;
                _damageOverlayActive = false;
                UI.SetOpacity(ref _damageOverlay, 0f);
            }
        }
    }

    private void System_OnSmokeGrenade(object sender, InGameHUDSystem.SmokeGrenadeArgs e)
    {
        UI.SetOpacity(ref _smokeEffect, e.isSmoke ? e.intensity : 0);
        UI.SetOpacity(ref _gasEffect, e.isSmoke ? 0 : e.intensity);
    }

    private void System_OnADSChange(object sender, InGameHUDSystem.ADSArgs e)
    {
        UI.SetOpacity(ref _sniperElement, e.isAiming ? 1f : 0f);
    }

    private void System_OnDamageTaken(object sender, DamageSourcePositionSystem.DamageIndicatorArgs args)
    {
        if (args.networkId == 0 || args.networkId != GetCurrentPlayerInfo().networkID)
        {
            return;
        }

        _damageOverlayActive = true;
        _damageOverlayTimer = _damageOverlayTime;
        UI.SetOpacity(ref _damageOverlay, 1f);
        
        DamageIndicatorElement damageIndicator = new();
        damageIndicator.transform.rotation = Quaternion.Euler(0f, 0f, args.angle);
        _crosshairElement.Add(damageIndicator);
    }

    private void System_OnPositionChanged(object sender, MinimapTeamLinkSystem.MinimapTeamArgs args)
    {
        if ((TeamSideType)args.TeamId == GetCurrentPlayerInfo().team)
        {
            VisualElement teamPlayerElement = _minimapMapElement.Q<VisualElement>("Team" + args.PlayerId.ToString());
            UI.SetActive(ref teamPlayerElement, args.Alive);
            UI.SetOpacity(ref teamPlayerElement, args.Alive ? 1f : 0f);
            Vector2 minimapPinPosition = GetMinimapPinPosition(args.Position);
            teamPlayerElement.style.left = new StyleLength(-minimapPinPosition.x + _minimapElement.resolvedStyle.width / 2);
            teamPlayerElement.style.top = new StyleLength(-minimapPinPosition.y + _minimapElement.resolvedStyle.height / 2);
            teamPlayerElement.transform.rotation = GetMinimapPinRotation(args.Forward);
        }
    }

    private void OnWinLoseRoundUpdate()
    {
        _winLoseRoundTimer += Time.deltaTime;

        float t = _winLoseRoundTimer / _winLoseRoundTime;

        if (t > 1f)
        {
            t = 1f;
        }

        UI.SetActive(ref _winLoseRoundElement, true);
        UI.SetOpacity(ref _winLoseRoundElement, 0f);

        if (t < .25f)
        {
            UI.SetOpacity(ref _winLoseRoundElement, t * 4f);
        }
        else if (t < .75f)
        {
            UI.SetOpacity(ref _winLoseRoundElement, 1f);
        }
        else if (t < 1f)
        {
            UI.SetOpacity(ref _winLoseRoundElement, 4f - t * 4f);
        }
        else
        {
            UI.SetOpacity(ref _winLoseRoundElement, 0f);
        }

        if (t == 1f)
        {
            UI.SetActive(ref _winLoseRoundElement, false);
            _winLoseRoundEngaged = false;
            _winLoseRoundElement.style.backgroundImage = null;
        }
    }
	
    private void OnTeamWinRound(object sender, RoundSystemClient.TeamWinRoundArgs args)
    {
        _winLoseRoundEngaged = true;
        _winLoseRoundTimer = 0f;
        ClientComponent currentPlayer = GetCurrentPlayerInfo();
        if (currentPlayer.networkID == 0)
        {
            return;
        }
        if (args.team == currentPlayer.team)
        {
            _winLoseRoundElement.style.backgroundImage = texData.GetTexture("hud_round_win");
        }
        else
        {
            _winLoseRoundElement.style.backgroundImage = texData.GetTexture("hud_round_lose");
        }
    }

    private float FlashIntensity(float x)
    {
        if (x > 0.68f) return 1f;

        x /= 0.68f; // Normalize to [0, 1] range

        return x == 0 ? 0 : x == 1 ? 1
              : x < 0.5 ? math.pow(2, 20 * x - 10) / 2
              : (2 - math.pow(2, -20 * x + 10)) / 2;
    }

    private void System_OnFlashGrenade(object sender, InGameHUDSystem.FlashGrenadeArgs args)
    {
        UI.SetOpacity(ref _flash, FlashIntensity(args.intensity));
    }

    //----------Start of Round Phase Functions
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

        if (roundComponent.gameWon)
        {
            ClientComponent currentPlayer = GetCurrentPlayerInfo();

            if (currentPlayer.networkID != 0)
            {
                _winLoseGameTimer += Time.deltaTime;

                float t = _winLoseGameTimer / _winLoseGameTime;
                if (t > 1f)
                {
                    t = 1f;
                }
                UI.SetActive(ref _winLoseGameElement, true);
                UI.SetOpacity(ref _winLoseGameElement, t);

                if (roundComponent.winners == currentPlayer.team)
                {
                    _winLoseGameElement.style.backgroundImage = texData.GetTexture("hud_game_win");
                }
                else
                {
                    _winLoseGameElement.style.backgroundImage = texData.GetTexture("hud_game_lose");
                }

                if (_winLoseGameTimer >= _winLoseEndGameTime)
                {
                    HandleGameOverAsync();
                }
            }
        }
    }

    public ClientComponent GetCurrentPlayerInfo()
    {
        ClientComponent currentPlayer = new();
        if (PlayerHelpers.GetClientPlayersByTeam(TeamSideType.Corpo).Count > 0)
        {
            ClientComponent firstCorpoPlayer = PlayerHelpers.GetClientPlayersByTeam(TeamSideType.Corpo).First();
            if (firstCorpoPlayer.playerID == ClientTransportHelper.instance.Session.CurrentPlayer.Id)
            {
                currentPlayer = firstCorpoPlayer;
            }
        }
        if (PlayerHelpers.GetClientPlayersByTeam(TeamSideType.Natif).Count > 0)
        {
            ClientComponent firstNatifPlayer = PlayerHelpers.GetClientPlayersByTeam(TeamSideType.Natif).First();
            if (firstNatifPlayer.playerID == ClientTransportHelper.instance.Session.CurrentPlayer.Id)
            {
                currentPlayer = firstNatifPlayer;
            }
        }
        return currentPlayer;
    }

    private async void HandleGameOverAsync()
    {
        await LoadUtils.QuitAsync();
        await LoadUtils.LoadSceneAsync("MainMenu", GameNetwork.SessionData.LoadingSteps.BackToMainMenu);
    }

    private void UpdateForBuyPhase(float t)
    {
        // Animation for Buy Phase
        // One forth of a second to pop up the visual
        // One half of a second to pop up Round Number and Buy Text

        UI.SetActive(ref _roundElement, true);
        UI.SetActive(ref _roundBuyPhaseText, true);

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
        UI.SetActive(ref _roundBuyPhaseText, false);
        UI.SetOpacity(ref _roundBuyPhaseText, 0f);

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
    //----------End of Round Phase Functions

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
    private void System_OnHealthChange(object sender, InGameHUDSystem.HealthArgs args)
    {
        _health.text = args.Health.ToString();
        _armor.text = args.armorLevel.ToString();
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
    private void System_OnHitRegistered(object sender, InGameHUDSystem.HitArgs args)
    {
        if (args.headHit)
        {
            _hitMarkerElement.style.backgroundImage = texData.GetTexture("hud_hit_head");
        }
        else
        {
            _hitMarkerElement.style.backgroundImage = texData.GetTexture("hud_hit_body");
        }
        _hitMarkerTimer = _hitMarkerTime;
        _hitRegistered = true;
    }
    private void System_OnPositionChanged(object sender, InGameHUDSystem.PositionArgs args)
    {
        // Set Position of the map element
        Vector2 minimapPinPosition = GetMinimapPinPosition(args.position);

        _minimapMapElement.style.left = new StyleLength(minimapPinPosition.x);
        _minimapMapElement.style.top = new StyleLength(minimapPinPosition.y);

        // Set Rotation of the player icon
        _minimapPlayerElement.transform.rotation = GetMinimapPinRotation(args.forward);
    }
    public Vector2 GetMinimapPinPosition(Vector3 worldPos)
    {
        Vector2 firstRefWorld = new(-36, 6); // First point of reference in the world
        Vector2 secondRefWorld = new(21, 14); // Second point of reference in the world
        Vector2 firstRefUI = new(0, -44); // First point of reference in the UI
        Vector2 secondRefUI = new(-220, -12); // Second point of reference in the UI

        Vector2 worldDelta = secondRefWorld - firstRefWorld; // World delta between the two points of reference
        Vector2 uiDelta = secondRefUI - firstRefUI; // UI delta between the two points of reference
        Vector2 scale = new(uiDelta.x / worldDelta.x, uiDelta.y / worldDelta.y); // Scale between the two points of reference

        Vector2 scaled = new(firstRefWorld.x * scale.x, firstRefWorld.y * scale.y); // Scaled position of the player relative to the two points of reference
        Vector2 offset = firstRefUI - scaled; // Offset between the two points of reference

        return new(worldPos.x * scale.x + offset.x, worldPos.z * scale.y + offset.y);
    }
    public Quaternion GetMinimapPinRotation(Vector3 forward)
    {
        return Quaternion.Euler(0f, 0f, math.degrees(math.atan2(forward.x, forward.z)));
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

    //----------Start of Player Icons Functions
    private void PlayerIconsUpdate(TeamSideType team, VisualElement teamIcons)
    {
        List<ClientComponent> players = PlayerHelpers.GetClientPlayersByTeam(team).OrderBy(x => x.networkID).ToList();
        //GetClientPlayersByTeam
        for (int i = 0; i < teamIcons.Children().Count(); i++)
        {
            VisualElement icon = teamIcons.Q<VisualElement>("Position" + (i + 1).ToString());
            if (i < players.Count)
            {

                if (players[i].playerID == ClientTransportHelper.instance.Session.CurrentPlayer.Id)
                {
                    UI.SetBorderColor(ref icon, Color.green);
                }
                else
                {
                    UI.SetBorderColor(ref icon, Color.gray);
                }
                UI.SetImageTintColor(ref icon, Color.white);
            }
            else
            {
                UI.SetBorderColor(ref icon, Color.clear);
                UI.SetImageTintColor(ref icon, Color.clear);
            }
        }
    }
    //----------End of Player Icons Functions

    //----------Start of KillFeed Functions
    private void InitializeNewKillFeed(object sender, KillFeedRPCSystem.KillDamageIndicatorArgs args)
    {
        KillFeedElement element = new()
        {
            KillerTeamSide = args.killer.team,
            KillerName = args.killer.name.ToString(),
            VictimTeamSide = args.target.team,
            VictimName = args.target.name.ToString()
        };
        element.Refresh();
        _killFeedContainer.Insert(0, element);

        Debug.Log($"{args.killer.name} killed {args.target.name}");
    }

    public void SetUIActive(bool value)
    {
        UI.SetActive(ref _HUD, value);
    }

    public bool IsUIActive()
    {
        return UI.IsActive(ref _HUD);
    }

    public UICentralController.UIState GetUIState()
    {
        return UICentralController.UIState.HUD;
    }
    //----------End of KillFeed Functions
}

[Serializable]
public struct WeaponMap
{
    public string Weapon;
    public Texture2D Tex;
}
