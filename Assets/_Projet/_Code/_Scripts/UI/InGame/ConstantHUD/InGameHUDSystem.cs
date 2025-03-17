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

    public event EventHandler<HealthArgs> HealthChangedEvent;
    public event EventHandler HitRegister;
    public event EventHandler<AmmoArgs> AmmoChangeEvent;


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
        foreach (var (currentHealth, hasHit, stuffInHandRef, stuffLListRef) in SystemAPI
            .Query<RefRO<CurrentHealthComponent>, RefRO<HasHitComponent>, RefRO<CharacterStuffInHandType>, RefRO<CharacterStuffList>> ()
            .WithAll<GhostOwnerIsLocal>())
        {
            HealthChangedEvent?.Invoke(this, new HealthArgs { Health = currentHealth.ValueRO.Value });

            if (hasHit.ValueRO.Value)
            {
                HitRegister?.Invoke(this, EventArgs.Empty);
            }

            if (SystemAPI.HasComponent<RangedWeaponDynamicData>(stuffLListRef.ValueRO.List[(int)stuffInHandRef.ValueRO.Value]))
            {
                var weaponData = SystemAPI.GetComponent<RangedWeaponDynamicData>(stuffLListRef.ValueRO.List[(int)stuffInHandRef.ValueRO.Value]);
                AmmoChangeEvent?.Invoke(this, new AmmoArgs { ammo = weaponData.currentAmmo, remainingAmmo = weaponData.remainingAmmo });
            }
        }
    }
}
