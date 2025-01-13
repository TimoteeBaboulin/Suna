using NaughtyAttributes;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [SerializeField] UIDocument healthArmorDocument;
    [SerializeField] UIDocument ammoDocument;

    [ReadOnly] public string C0 = "D : Damage (Reduce Health)";
    [ReadOnly] public string C1 = "H : Heal (Increase Health)";
    [ReadOnly] public string C2 = "P : Pierce (Decrease Armor)";
    [ReadOnly] public string C3 = "U : Undamage (Increase Armor";
    [ReadOnly] public string C4 = "Mouse0 : Shoot (Reduce Ammo)";
    [ReadOnly] public string C5 = "R : Reload (Set Ammo to Capacity)";
    [ReadOnly] public string C6 = "C : Change (Decrease Capacity)";
    [ReadOnly] public string C7 = "V : Value (Increase Capacity)";

    VisualElement healthArmor;
    VisualElement ammoLeftCapacity;

    Label health;
    Label armor;
    Label ammo;
    Label capacity;

    private void Awake()
    {
        healthArmor = healthArmorDocument.rootVisualElement;
        ammoLeftCapacity = ammoDocument.rootVisualElement;

        health = healthArmor.Q<Label>("HealthLabel");
        armor = healthArmor.Q<Label>("ArmorLabel");

        ammo = ammoLeftCapacity.Q<Label>("AmmoLeftLabel");
        capacity = ammoLeftCapacity.Q<Label>("AmmoCapacityLabel");

        TestPlayerDataSystem system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TestPlayerDataSystem>();
        system.OnHealthChange += System_OnHealthChange;
        system.OnArmorChange += System_OnArmorChange;
        system.OnAmmoChange += System_OnAmmoChange;
        system.OnCapacityChange += System_OnCapacityChange;
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
}
