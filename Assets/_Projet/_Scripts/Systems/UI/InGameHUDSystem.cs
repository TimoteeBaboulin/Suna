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

    public event EventHandler<HealthArgs> HealthChangedEvent;
    public event EventHandler HitRegister;
    public event EventHandler<AmmoArgs> AmmoChangeEvent;
    public event EventHandler<MoneyArgs> MoneyChangedEvent;


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

        foreach (var (currentHealth, client, hasHit, stuffListRef) in SystemAPI
            .Query<RefRO<CurrentHealthComponent>, RefRO<CharacterClientAttachedComponent>, RefRO<HasHitComponent>, RefRO<CharacterStuffList>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            HealthChangedEvent?.Invoke(this, new HealthArgs { Health = (int)currentHealth.ValueRO.Value });
            uint money = SystemAPI.GetComponent<CharacterMoney>(client.ValueRO.ClientEntity).money;
            MoneyChangedEvent?.Invoke(this, new MoneyArgs { money = money });

            if (hasHit.ValueRO.Value)
            {
                HitRegister?.Invoke(this, EventArgs.Empty);
            }
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
            .Query<RefRO<StuffOwner>>()
            .WithAll<GhostOwnerIsLocal, IsStuffInHand>()
            .WithNone<RangedWeaponDynamicData>()
            .WithEntityAccess())
        {
            AmmoChangeEvent?.Invoke(this, new AmmoArgs { ammo = 0, remainingAmmo = 0 });
        }
    }
}
