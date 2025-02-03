using System;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    private UIDocument _HUDDocument;

    private VisualElement _HUD;

    private VisualElement _crosshairElement;

    private Label _health;
    private Label _armor;
    private Label _ammo;
    private Label _capacity;

    private Label _corpoScore;
    private Label _natifScore;

    private bool _hitRegistered = false;

    private InGameHUDSystem _inGameHUDSystem = null;

    private StyleColor _crosshairBaseColor;

    private void Awake()
    {
        _HUDDocument = GetComponent<UIDocument>();
        _HUD = _HUDDocument.rootVisualElement;

        _health = _HUD.Q<Label>("HealthLabel");
        _armor = _HUD.Q<Label>("ArmorLabel");

        _ammo = _HUD.Q<Label>("AmmoLeftLabel");
        _capacity = _HUD.Q<Label>("AmmoCapacityLabel");

        _crosshairElement = _HUD.Q("Crosshair");
        _crosshairBaseColor = _crosshairElement.style.unityBackgroundImageTintColor;

        _corpoScore = _HUD.Q<Label>("CorpoScore");
        _natifScore = _HUD.Q<Label>("NatifScore");
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
        _crosshairElement.style.unityBackgroundImageTintColor = _crosshairBaseColor;
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
