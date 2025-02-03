using System;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [SerializeField] private UIDocument _healthArmorDocument;
    [SerializeField] private UIDocument _ammoDocument;
    [SerializeField] private UIDocument _crosshairDocument;
    [SerializeField] private UIDocument _teamsDocument;
    [SerializeField] private UIDocument _timerDocument;

    private VisualElement _healthArmor;
    private VisualElement _ammoLeftCapacity;
    private VisualElement _crosshairElement;

    private Label _health;
    private Label _armor;
    private Label _ammo;
    private Label _capacity;

    private Label _corpoScore;
    private Label _natifScore;

    private bool _hitRegistered = false;

    private InGameHUDSystem _inGameHUDSystem = null;

    private void Awake()
    {
        _healthArmor = _healthArmorDocument.rootVisualElement;
        _ammoLeftCapacity = _ammoDocument.rootVisualElement;

        _health = _healthArmor.Q<Label>("HealthLabel");
        _armor = _healthArmor.Q<Label>("ArmorLabel");

        _ammo = _ammoLeftCapacity.Q<Label>("AmmoLeftLabel");
        _capacity = _ammoLeftCapacity.Q<Label>("AmmoCapacityLabel");

        _crosshairElement = _crosshairDocument.rootVisualElement.Q("Crosshair");

        _corpoScore = _teamsDocument.rootVisualElement.Q<Label>("CorpoScore");
        _natifScore = _teamsDocument.rootVisualElement.Q<Label>("NatifScore");
    }

    private void Update()
    {
        if (_inGameHUDSystem == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            _inGameHUDSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InGameHUDSystem>();
            _inGameHUDSystem.HealthChangedEvent += System_OnHealthChange;
            _inGameHUDSystem.HitRegister += System_OnHitRegistered;
        }

        if (_hitRegistered)
        {
            _hitRegistered = false;
            StartCoroutine(HitRegistered());
        }
    }

    IEnumerator HitRegistered()
    {
        yield return new WaitForSeconds(1f);
        _crosshairElement.style.unityBackgroundImageTintColor = new StyleColor(Color.black);
        yield return null;
    }

    private void System_OnHealthChange(object sender, InGameHUDSystem.HealthArgs args)
    {
        if (args is InGameHUDSystem.HealthArgs arg) _health.text = arg.Health.ToString();
    }
    private void System_OnArmorChange(object sender, TestPlayerDataSystem.ArmorArgs args)
    {
        //if (args is TestPlayerDataSystem.ArmorArgs arg) armor.text = arg.Armor.ToString();
    }
    private void System_OnAmmoChange(object sender, TestPlayerDataSystem.AmmoArgs args)
    {
        // if (args is TestPlayerDataSystem.AmmoArgs arg) ammo.text = arg.Ammo.ToString();
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
}
