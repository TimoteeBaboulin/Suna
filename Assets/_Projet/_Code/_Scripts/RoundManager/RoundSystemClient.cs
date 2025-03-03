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
        //Only run if we're in Client world
        //if (state.World != ConnectionManager.Instance.Client)
        //{
        //    _running = false;
        //    return;
        //}

        _running = true;

        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithNone<ServerDataComponent>();
        state.RequireForUpdate(state.GetEntityQuery(builder));

        EntityQueryBuilder roundBuilder = new EntityQueryBuilder(Allocator.Temp).WithAll<RoundComponent>();
        EntityQuery query = state.GetEntityQuery(builder);

        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        Debug.Log("Found " + entityArray.Length + " Round Components.");

        //Initialize round timer to buy phase value
        foreach ((RefRW<RoundComponent> reference, Entity entity) in SystemAPI.Query<RefRW<RoundComponent>>().WithEntityAccess())
        {
            var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);
            reference.ValueRW.timer = buffer[0];
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!_running) return;

        //Get the Read/Write Reference of the component for use in functions
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<RoundComponent>().Build(ref state);
        RefRW<RoundComponent> round;
        if (!query.TryGetSingletonRW(out round))
            return;

        //Prepare the buffer to use at the end of the update to avoid breaking the reference to the component
        EntityCommandBuffer buffer = new EntityCommandBuffer(Allocator.Temp);

        //Basic timer tick
        round.ValueRW.timer -= Time.deltaTime;
        if (round.ValueRW.timer < 0)
        {
            round.ValueRW.timer = 0;
        }

        //Read score change RPCs
        foreach(var (rpcComponent, newRoundComponent, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<VictoryRpcCommand>>().WithEntityAccess())
        {
            ChangeScore(ref state, newRoundComponent.ValueRO.team, round);
            buffer.DestroyEntity(entity);
        }

        //Read phase change RPCs
        foreach (var (rpcComponent, newRoundComponent, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ChangePhaseRpcCommand>>().WithEntityAccess())
        {
            ChangePhase(ref state, newRoundComponent.ValueRO.phase, query.GetSingletonEntity(), round);
            buffer.DestroyEntity(entity);
        }

        buffer.Playback(state.EntityManager);
        buffer.Dispose();
    }

    public void ChangeScore(ref SystemState state, TimoteeTeam team, RefRW<RoundComponent> component) {
        //Update the score and loss streak of the corresponding teams
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
        //Update the timer and phases after receiving an update
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

        component.ValueRW.currentPhase = phase;
        component.ValueRW.timer = buffer[(int)phase];
    }
}
