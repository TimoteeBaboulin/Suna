using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using static RoundSystemServer;

partial struct RoundSystemClient : IRoundManager, ISystem
{
    private bool _running;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        if (state.World != ConnectionManager.Instance.Client)
        {
            _running = false;
            return;
        }

        RoundComponent roundComponent = SystemAPI.GetSingleton<RoundComponent>();
        Entity entity = SystemAPI.GetSingletonEntity<RoundComponent>();
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

        roundComponent.timer = buffer[0];
        IRoundManager._currentTime = buffer[0];
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!_running) return;

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<RoundComponent>().Build(ref state);
        RefRW<RoundComponent> round = query.GetSingletonRW<RoundComponent>();
        EntityCommandBuffer buffer = new EntityCommandBuffer(Allocator.Temp);

        round.ValueRW.timer -= Time.deltaTime;
        if (round.ValueRW.timer < 0)
        {
            round.ValueRW.timer = 0;
        }


        foreach(var (rpcComponent, newRoundComponent, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<VictoryRpcCommand>>().WithEntityAccess())
        {
            ChangeScore(ref state, newRoundComponent.ValueRO.team, ref round.ValueRW);
            buffer.DestroyEntity(entity);
        }

        foreach (var (rpcComponent, newRoundComponent, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ChangePhaseRpcCommand>>().WithEntityAccess())
        {
            ChangePhase(ref state, newRoundComponent.ValueRO.phase, query.GetSingletonEntity(), ref round.ValueRW);
            buffer.DestroyEntity(entity);
        }

        buffer.Playback(state.EntityManager);
        buffer.Dispose();

        RoundComponent roundComponent = SystemAPI.GetSingleton<RoundComponent>();
        IRoundManager._currentTime = roundComponent.timer;
    }

    public void ChangeScore(ref SystemState state, TimoteeTeam team, ref RoundComponent component) {
        switch (team)
        {
            case TimoteeTeam.Corporation:
                component.corporationScore++;
                component.nativeLossStreak = Math.Min(component.nativeLossStreak + 1, component.maxStreakCount);
                component.corporationLossStreak = 0;

                break;

            case TimoteeTeam.Natives:
                component.nativeScore++;
                component.corporationLossStreak = Math.Min(component.corporationLossStreak + 1, component.maxStreakCount);
                component.nativeLossStreak = 0;

                break;
        }
    }
    public void ChangePhase(ref SystemState state, RoundPhase phase, Entity entity, ref RoundComponent component)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

        component.currentPhase = phase;
        component.timer = buffer[(int)phase];

        if (phase == RoundPhase.BuyPhase)
        {
            IRoundManager.OnRoundStart?.Invoke(component.corporationScore, component.nativeScore);
        }
        else if (phase == RoundPhase.PostPlantPhase)
        {
            IRoundManager.OnCollectorPlanted?.Invoke();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
