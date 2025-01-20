using NaughtyAttributes;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [SerializeField] UIDocument healthArmorDocument;
    [SerializeField] UIDocument ammoDocument;
    [SerializeField] UIDocument cashDocument;

    [ReadOnly] public string C0 = "D : Damage (Reduce Health)";
    [ReadOnly] public string C1 = "H : Heal (Increase Health)";
    [ReadOnly] public string C2 = "P : Pierce (Decrease Armor)";
    [ReadOnly] public string C3 = "U : Undamage (Increase Armor";
    [ReadOnly] public string C4 = "Mouse0 : Shoot (Reduce Ammo)";
    [ReadOnly] public string C5 = "R : Reload (Set Ammo to Capacity)";
    [ReadOnly] public string C6 = "C : Change (Decrease Capacity)";
    [ReadOnly] public string C7 = "V : Value (Increase Capacity)";
    [ReadOnly] public string C8 = "B : Buy (Decrease Cash)";
    [ReadOnly] public string C9 = "G : Gain (Increase Cash)";

    VisualElement healthArmor;
    VisualElement ammoLeftCapacity;
    VisualElement cashElement;

    Label health;
    Label armor;
    Label ammo;
    Label capacity;
    Label cash;

    private void Awake()
    {
        healthArmor = healthArmorDocument.rootVisualElement;
        ammoLeftCapacity = ammoDocument.rootVisualElement;
        cashElement = cashDocument.rootVisualElement;

        health = healthArmor.Q<Label>("HealthLabel");
        armor = healthArmor.Q<Label>("ArmorLabel");
        cash = cashElement.Q<Label>("Cash");

        ammo = ammoLeftCapacity.Q<Label>("AmmoLeftLabel");
        capacity = ammoLeftCapacity.Q<Label>("AmmoCapacityLabel");

        TestPlayerDataSystem system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TestPlayerDataSystem>();
        system.OnHealthChange += System_OnHealthChange;
        system.OnArmorChange += System_OnArmorChange;
        system.OnAmmoChange += System_OnAmmoChange;
        system.OnCapacityChange += System_OnCapacityChange;
        system.OnCashChange += System_OnCashChange;
    }
    private void System_OnHealthChange(object sender, TestPlayerDataSystem.HealthArgs e)
    {
        if (e is TestPlayerDataSystem.HealthArgs arg) health.text = arg.Health.ToString();
    }
    private void System_OnArmorChange(object sender, TestPlayerDataSystem.ArmorArgs e)
    {
        if (e is TestPlayerDataSystem.ArmorArgs arg) armor.text = arg.Armor.ToString();
    }
    private void System_OnAmmoChange(object sender, TestPlayerDataSystem.AmmoArgs e)
    {
        if (e is TestPlayerDataSystem.AmmoArgs arg) ammo.text = arg.Ammo.ToString();
    }
    private void System_OnCapacityChange(object sender, TestPlayerDataSystem.CapacityArgs e)
    {
        if (e is TestPlayerDataSystem.CapacityArgs arg) capacity.text = arg.Capacity.ToString();
    }

    private void System_OnCashChange(object sender, TestPlayerDataSystem.MoneyArgs e)
    {
        if (e is TestPlayerDataSystem.MoneyArgs arg) cash.text = arg.Cash.ToString();
    }
}
