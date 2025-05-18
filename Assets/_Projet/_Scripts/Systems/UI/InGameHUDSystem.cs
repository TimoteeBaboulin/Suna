using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.VisualScripting;
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
    public class SmokeGrenadeArgs : EventArgs { public float intensity; public bool isSmoke; }
    public class PositionArgs : EventArgs { public float3 position; public float3 forward; }
    public class ADSArgs : EventArgs { public bool isAiming; }

    public event EventHandler<HealthArgs> HealthChangedEvent;
    public event EventHandler<HitArgs> HitRegister;
    public event EventHandler<AmmoArgs> AmmoChangeEvent;
    public event EventHandler<MoneyArgs> MoneyChangedEvent;
    public event EventHandler<FlashGrenadeArgs> FlashGrenadeEvent;
    public event EventHandler<SmokeGrenadeArgs> SmokeGrenadeEvent;
    public event EventHandler<PositionArgs> PositionChangedEvent;
    public event EventHandler<ADSArgs> ADSChangedEvent;

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
            HealthChangedEvent?.Invoke(this, new HealthArgs { Health = math.max((int)currentHealth.ValueRO.Value, 0) });
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

        foreach(var (smokeEffect, entity) in SystemAPI
            .Query<RefRO<SmokeGrenadeEffect>>()
            .WithAll<GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            SmokeGrenadeEvent?.Invoke(this, new SmokeGrenadeArgs { intensity = smokeEffect.ValueRO.intensity, isSmoke = smokeEffect.ValueRO.isSmoke });
        }

        foreach (var (adsing, entity) in SystemAPI
            .Query<RefRO<CharacterComponent>>()
            .WithAll<GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            if (!TryGetCurrentlyEquippedStuff(entity, out Entity stuffEntity))
                continue;

            if (SystemAPI.HasComponent<StuffDatabaseAccess>(stuffEntity))
            {
                StuffDatabaseAccess databaseAccess = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffEntity);
                GameResourcesDatabase database = SystemAPI.GetSingleton<GameResourcesDatabase>();

                ADSChangedEvent?.Invoke(this, new ADSArgs { isAiming = adsing.ValueRO.isAiming && databaseAccess.GetData(ref database).canADS }); 
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
            .Query<RefRO<StuffDynamicData>>()
            .WithAll<GhostOwnerIsLocal, IsStuffInHand>()
            .WithNone<RangedWeaponDynamicData>()
            .WithEntityAccess())
        {
            AmmoChangeEvent?.Invoke(this, new AmmoArgs { ammo = 0, remainingAmmo = 0 });
        }

        foreach(var localTransform in SystemAPI
            .Query<RefRO<LocalTransform>>()
            .WithAll<GhostOwnerIsLocal, CharacterComponent>())
        {
            PositionChangedEvent?.Invoke(this, new PositionArgs { position = localTransform.ValueRO.Position, forward = localTransform.ValueRO.Forward() });
        }
    }

    bool TryGetCurrentlyEquippedStuff(Entity characterEntity, out Entity stuffEntity)
    {
        stuffEntity = default;

        if (!SystemAPI.HasBuffer<CharacterStuffList>(characterEntity))
            return false;

        DynamicBuffer<CharacterStuffList> stuffList = SystemAPI.GetBuffer<CharacterStuffList>(characterEntity);
        CharacterStuffInfos stuffInfos = SystemAPI.GetComponent<CharacterStuffInfos>(characterEntity);
        stuffEntity = StuffUtils.GetStuffInHand(stuffList, stuffInfos);
        return true;
    }
}
