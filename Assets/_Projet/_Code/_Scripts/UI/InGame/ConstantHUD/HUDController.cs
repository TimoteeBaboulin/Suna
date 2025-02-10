using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
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

    private Label _corpoScore;
    private Label _natifScore;

    private bool _hitRegistered = false;

    private InGameHUDSystem _inGameHUDSystem = null;

    private StyleColor _crosshairBaseColor;

    // Weapon Slot
    private VisualElement _weaponContainer;
    [SerializeField] private VisualTreeAsset _weaponAsset;
    [SerializeField] List<WeaponMap> _weaponMap;
    [SerializeField] List<WeaponSlot> _weaponSlot;
    private int selectedSlot = 0;


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

        _weaponContainer = _HUD.Q<VisualElement>("WeaponContainer");

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
