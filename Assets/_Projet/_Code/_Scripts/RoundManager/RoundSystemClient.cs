using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using static RoundSystemServer;

partial struct RoundSystemClient : ISystem
{
    private bool _running;

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
            ChangeScore(ref state, newRoundComponent.ValueRO.team, round);
            buffer.DestroyEntity(entity);
        }

        foreach (var (rpcComponent, newRoundComponent, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ChangePhaseRpcCommand>>().WithEntityAccess())
        {
            ChangePhase(ref state, newRoundComponent.ValueRO.phase, query.GetSingletonEntity(), round);
            buffer.DestroyEntity(entity);
        }

        buffer.Playback(state.EntityManager);
        buffer.Dispose();

        RoundComponent roundComponent = SystemAPI.GetSingleton<RoundComponent>();
    }

    public void ChangeScore(ref SystemState state, TimoteeTeam team, RefRW<RoundComponent> component) {
        switch (team)
        {
            case TimoteeTeam.Corporation:
                component.ValueRW.corporationScore++;
                component.ValueRW.nativeLossStreak = Math.Min(component.ValueRW.nativeLossStreak + 1, component.ValueRW.maxStreakCount);
                component.ValueRW.corporationLossStreak = 0;

                break;

            case TimoteeTeam.Natives:
                component.ValueRW.nativeScore++;
                component.ValueRW.corporationLossStreak = Math.Min(component.ValueRW.corporationLossStreak + 1, component.ValueRW.maxStreakCount);
                component.ValueRW.nativeLossStreak = 0;

                break;
        }
    }
    public void ChangePhase(ref SystemState state, RoundPhase phase, Entity entity, RefRW<RoundComponent> component)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

        component.ValueRW.currentPhase = phase;
        component.ValueRW.timer = buffer[(int)phase];
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
