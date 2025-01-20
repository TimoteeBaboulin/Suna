using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public partial struct RoundManager : ISystem, ISystemStartStop
{
    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<RoundComponent>();
        EntityQuery query = builder.Build(ref state);
        var entity = query.GetSingletonEntity();
        RoundComponent component = SystemAPI.GetSingleton<RoundComponent>();

        InitGame(ref state, entity, ref component);
        SystemAPI.SetSingleton<RoundComponent>(component);
    }

    public void OnStopRunning(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<RoundComponent>(out var roundComponent))
        {
            throw new System.Exception("Couldn't find RoundComponent Singleton, please check that there is a single RoundManager in the world.");
        }

        var queryBuilder = new EntityQueryBuilder(Allocator.Temp);
        var query = queryBuilder.WithAll<RoundComponent>().Build(ref state);

        roundComponent.timer -= Time.deltaTime;
        if (roundComponent.timer < 0)
        {
            TimeOutPhase(ref state, query.GetSingletonEntity(), ref roundComponent);
        }
        SystemAPI.SetSingleton<RoundComponent>(roundComponent);
    }

    private void TimeOutPhase(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        component.currentPhase++;

        if (component.currentPhase > RoundPhase.PostRoundPhase)
        {
            InitRound(ref state, entity, ref component);
            component.currentPhase = RoundPhase.BuyPhase;
        }

        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);
        component.timer = buffer[(int)component.currentPhase];
    }

    private void InitGame(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);
        component.currentPhase = RoundPhase.BuyPhase;
        component.timer = buffer[(int)component.currentPhase];
    }

    private void InitRound(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        Debug.Log("New round");
    }
}