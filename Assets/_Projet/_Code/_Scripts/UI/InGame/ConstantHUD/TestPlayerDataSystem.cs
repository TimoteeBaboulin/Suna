using System;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[BurstCompile]
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

    protected override void OnCreate()
    {
        RequireForUpdate<TestPlayerData>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            foreach (RefRW<TestPlayerData> data in SystemAPI.Query<RefRW<TestPlayerData>>())
            {
                if (data.ValueRO.Health > 0) data.ValueRW.Health = data.ValueRO.Health - 5;
                OnHealthChange?.Invoke(this, new HealthArgs { Health = data.ValueRO.Health });
            }
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            foreach (RefRW<TestPlayerData> data in SystemAPI.Query<RefRW<TestPlayerData>>())
            {
                if (data.ValueRO.Health < 100) data.ValueRW.Health = data.ValueRO.Health + 5;
                OnHealthChange?.Invoke(this, new HealthArgs { Health = data.ValueRO.Health });
            }
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            foreach (RefRW<TestPlayerData> data in SystemAPI.Query<RefRW<TestPlayerData>>())
            {
                if (data.ValueRO.Armor > 0) data.ValueRW.Armor = data.ValueRO.Armor - 5;
                OnArmorChange?.Invoke(this, new ArmorArgs { Armor = data.ValueRO.Armor });
            }
        }
        else if (Input.GetKeyDown(KeyCode.U))
        {
            foreach (RefRW<TestPlayerData> data in SystemAPI.Query<RefRW<TestPlayerData>>())
            {
                if (data.ValueRO.Armor < 95) data.ValueRW.Armor = data.ValueRO.Armor + 5;
                OnArmorChange?.Invoke(this, new ArmorArgs { Armor = data.ValueRO.Armor });
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            foreach (RefRW<TestPlayerData> data in SystemAPI.Query<RefRW<TestPlayerData>>())
            {
                if (data.ValueRO.AmmoLeft > 0) data.ValueRW.AmmoLeft = data.ValueRO.AmmoLeft - 1;
                OnAmmoChange?.Invoke(this, new AmmoArgs { Ammo = data.ValueRO.AmmoLeft });
            }
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (RefRW<TestPlayerData> data in SystemAPI.Query<RefRW<TestPlayerData>>())
            {
                data.ValueRW.AmmoLeft = data.ValueRO.AmmoCapacity;
                OnAmmoChange?.Invoke(this, new AmmoArgs { Ammo = data.ValueRO.AmmoLeft });
            }
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            foreach (RefRW<TestPlayerData> data in SystemAPI.Query<RefRW<TestPlayerData>>())
            {
                if (data.ValueRO.AmmoCapacity > 10) data.ValueRW.AmmoCapacity = data.ValueRO.AmmoCapacity - 10;
                data.ValueRW.AmmoLeft = data.ValueRO.AmmoCapacity;
                OnCapacityChange?.Invoke(this, new CapacityArgs { Capacity = data.ValueRO.AmmoCapacity });
                OnAmmoChange?.Invoke(this, new AmmoArgs { Ammo = data.ValueRO.AmmoLeft });
            }
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            foreach (RefRW<TestPlayerData> data in SystemAPI.Query<RefRW<TestPlayerData>>())
            {
                if (data.ValueRO.AmmoCapacity < 200) data.ValueRW.AmmoCapacity = data.ValueRO.AmmoCapacity + 10;
                data.ValueRW.AmmoLeft = data.ValueRO.AmmoCapacity;
                OnCapacityChange?.Invoke(this, new CapacityArgs { Capacity = data.ValueRO.AmmoCapacity });
                OnAmmoChange?.Invoke(this, new AmmoArgs { Ammo = data.ValueRO.AmmoLeft });
            }
        }
    }
}
