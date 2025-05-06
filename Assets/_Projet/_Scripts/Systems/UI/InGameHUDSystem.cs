using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class InGameHUDSystem : SystemBase
{
    public class HealthArgs : EventArgs { public int Health; }
    public class AmmoArgs : EventArgs { public int ammo; public int remainingAmmo; }
    public class MoneyArgs : EventArgs { public uint money; }
    public class HitArgs : EventArgs { public bool headHit; }
    public class FlashGrenadeArgs : EventArgs { public float intensity; }

    public event EventHandler<HealthArgs> HealthChangedEvent;
    public event EventHandler<HitArgs> HitRegister;
    public event EventHandler<AmmoArgs> AmmoChangeEvent;
    public event EventHandler<MoneyArgs> MoneyChangedEvent;
    public event EventHandler<FlashGrenadeArgs> FlashGrenadeEvent;

    [BurstCompile]
    protected override void OnCreate()
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<CurrentHealthComponent, HasHitComponent, CharacterStuffList>();

        RequireForUpdate(GetEntityQuery(builder));
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        foreach (var (currentHealth, charaMoney, hasHit, stuffListRef) in SystemAPI
            .Query<RefRO<CurrentHealthComponent>, RefRO<CharacterMoney>, RefRO<HasHitComponent>, DynamicBuffer<CharacterStuffList>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            HealthChangedEvent?.Invoke(this, new HealthArgs { Health = (int)currentHealth.ValueRO.Value });
            uint money = charaMoney.ValueRO.money;
            MoneyChangedEvent?.Invoke(this, new MoneyArgs { money = money });

            if (hasHit.ValueRO.Value)
            {
                HitRegister?.Invoke(this, new HitArgs { headHit = hasHit.ValueRO.HeadHit });
            }
        }

        foreach(var (flashEffect, entity) in SystemAPI
            .Query<RefRO<FlashGrenadeEffect>>()
            .WithAll<GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            FlashGrenadeEvent?.Invoke(this, new FlashGrenadeArgs { intensity = flashEffect.ValueRO.intensity });
        }

        foreach (var (weaponDataRef, stuff) in SystemAPI
            .Query<RefRO<RangedWeaponDynamicData>>()
            .WithAll<GhostOwnerIsLocal, IsStuffInHand>()
            .WithEntityAccess())
        {
            ref readonly RangedWeaponDynamicData weaponData = ref weaponDataRef.ValueRO;
            AmmoChangeEvent?.Invoke(this, new AmmoArgs { ammo = weaponData.currentAmmo, remainingAmmo = weaponData.remainingAmmo });
        }

        foreach (var (stuffOwner, stuff) in SystemAPI
            .Query<RefRO<StuffDynamicData>>()
            .WithAll<GhostOwnerIsLocal, IsStuffInHand>()
            .WithNone<RangedWeaponDynamicData>()
            .WithEntityAccess())
        {
            AmmoChangeEvent?.Invoke(this, new AmmoArgs { ammo = 0, remainingAmmo = 0 });
        }
    }
}
