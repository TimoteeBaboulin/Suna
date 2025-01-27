using System;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [SerializeField] UIDocument healthArmorDocument;
    [SerializeField] UIDocument ammoDocument;
    [SerializeField] UIDocument crosshairDocument;

    VisualElement healthArmor;
    VisualElement ammoLeftCapacity;
    VisualElement crosshairElement;

    Label health;
    Label armor;
    Label ammo;
    Label capacity;

    bool hitRegistered = false;

    private InGameHUDSystem system = null;

    private void Awake()
    {
        healthArmor = healthArmorDocument.rootVisualElement;
        ammoLeftCapacity = ammoDocument.rootVisualElement;

        health = healthArmor.Q<Label>("HealthLabel");
        armor = healthArmor.Q<Label>("ArmorLabel");

        ammo = ammoLeftCapacity.Q<Label>("AmmoLeftLabel");
        capacity = ammoLeftCapacity.Q<Label>("AmmoCapacityLabel");

        crosshairElement = crosshairDocument.rootVisualElement.Q("Crosshair");
    }

    private void Update()
    {
        if (system == null && World.DefaultGameObjectInjectionWorld.Name == "ClientWorld")
        {
            system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InGameHUDSystem>();
            system.HealthChangedEvent += System_OnHealthChange;
            system.HitRegister += System_OnHitRegistered;
        }
        if (hitRegistered)
        {
            hitRegistered = false;
            StartCoroutine(HitRegistered());
        }
    }

    IEnumerator HitRegistered()
    {
        yield return new WaitForSeconds(1f);
        crosshairElement.style.unityBackgroundImageTintColor = new StyleColor(Color.black);
        yield return null;
    }

    private void System_OnHealthChange(object sender, InGameHUDSystem.HealthArgs args)
    {
        if (args is InGameHUDSystem.HealthArgs arg) health.text = arg.Health.ToString();
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
        hitRegistered = true;
        crosshairElement.style.unityBackgroundImageTintColor = new StyleColor(Color.red);
    }
}
