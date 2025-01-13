using System;
using Unity.Entities;
using UnityEngine;

public partial class TestPlayerDataSystem : SystemBase
{
    public class HealthArgs : EventArgs { public uint Health; }
    public class ArmorArgs : EventArgs { public uint Armor; }
    public class AmmoArgs : EventArgs { public uint Ammo; }
    public class CapacityArgs : EventArgs { public uint Capacity; }

    public event EventHandler<HealthArgs> OnHealthChange;
    public event EventHandler<ArmorArgs> OnArmorChange;
    public event EventHandler<AmmoArgs> OnAmmoChange;
    public event EventHandler<CapacityArgs> OnCapacityChange;

    protected override void OnUpdate()
    {
        
    }
}
