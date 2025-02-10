using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Matchmaker.Models;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

public partial struct RoundSystemServer : ISystem, ISystemStartStop
{
    public struct VictoryRpcCommand : IRpcCommand
    {
        public TimoteeTeam team;
    }

    public struct ChangePhaseRpcCommand : IRpcCommand
    {
        public RoundPhase phase;
    }

    private EntityQuery _query;
    public enum TimoteeTeam : byte //TODO: Switch to a normalized enum for the whole project
    {
        Corporation,
        Natives
    };

    private bool _running; //TODO: Add a server and/or client component to switch to RequireForUpdate

    //public Action<int, int> OnRoundStart;
    //public Action OnCollectorPlanted;

    //[BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        //TODO: Switch to a RequireForUpdate to avoid performance drops
        if (state.World != ConnectionManager.Instance.Server)
        {
            _running = false;
            return;
        }

        _running = true;

        //Create the query and store it for future use
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<RoundComponent>();
        _query = builder.Build(ref state);

        RefRW<RoundComponent> roundComponent = _query.GetSingletonRW<RoundComponent>();

        //Get the necessary references to set up the start of the game
        var entity = _query.GetSingletonEntity();

        InitGame(ref state, entity, roundComponent);
        //IRoundManager._currentTime = roundComponent.ValueRW.timer;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!_running) return;

        RefRW<RoundComponent> roundComponent = _query.GetSingletonRW<RoundComponent>();

        //Check if the bomb was planted
        Entity entity = _query.GetSingletonEntity();
        if (roundComponent.ValueRW.currentPhase == RoundPhase.ActionPhase && state.EntityManager.HasComponent<RoundCollectorPlantedComponent>(entity))
        {
            CollectorPlanted(ref state, entity, roundComponent);
        }
        else
        {
            //Update the timer and change to next phase in case the timer runs out
            roundComponent.ValueRW.timer -= Time.deltaTime;
            if (roundComponent.ValueRW.timer < 0)
            {
                TimeOutPhase(ref state, entity, roundComponent);
            }
        }

        //IRoundManager._currentTime = roundComponent.ValueRW.timer;
    }

    private void Victory(ref SystemState state, Entity entity, RefRW<RoundComponent> component, TimoteeTeam team)
    {
        if (team == TimoteeTeam.Corporation)
        {
            component.ValueRW.corporationScore++;
            component.ValueRW.corporationLossStreak = 0;

            if (component.ValueRW.nativeLossStreak < component.ValueRW.maxStreakCount)
                component.ValueRW.nativeLossStreak++;
        }
        else
        {
            component.ValueRW.nativeScore++;
            component.ValueRW.nativeLossStreak = 0;

            if (component.ValueRW.corporationLossStreak < component.ValueRW.maxStreakCount)
                component.ValueRW.corporationLossStreak++;
        }

        VictoryRpcCommand rpc = new() { team = team };
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<InitializedClient>().Build(ref state);
        EntityManager entityManager = state.WorldUnmanaged.EntityManager;
        foreach (var client in query.ToEntityArray(Allocator.Temp))
        {
            Entity rpcEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest() { TargetConnection = client });
            entityManager.AddComponentData(rpcEntity, rpc);
        }

        entityManager.AddComponent<ScoreChangedComponent>(entity);
    }

    private void TimeOutPhase(ref SystemState state, Entity entity, RefRW<RoundComponent> component)
    {
        //Gets to next phase because of time out
        if (component.ValueRW.currentPhase == RoundPhase.ActionPhase)
        {
            Victory(ref state, entity, component, TimoteeTeam.Natives);
            component.ValueRW.currentPhase = RoundPhase.PostRoundPhase; 
        }
        else
        {
            if (component.ValueRW.currentPhase == RoundPhase.PostPlantPhase)
                Victory(ref state, entity, component, TimoteeTeam.Corporation);
            component.ValueRW.currentPhase++;
        }

        //If the round ended, get to next one
        if (component.ValueRW.currentPhase > RoundPhase.PostRoundPhase)
        {
            InitRound(ref state, entity, component);
        }

        //Sets the timer for the new phase
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);
        component.ValueRW.timer = buffer[(int)component.ValueRW.currentPhase];

        SendCurrentPhase(ref state, entity, component);
    }

    private void InitGame(ref SystemState state, Entity entity, RefRW<RoundComponent> component)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);
        component.ValueRW.currentPhase = RoundPhase.BuyPhase;
        component.ValueRW.timer = buffer[(int)component.ValueRW.currentPhase];
        component.ValueRW.currentRound = 0;
        component.ValueRW.nativeScore = 0;
        component.ValueRW.corporationScore = 0;
        InitRound(ref state, entity, component);
    }

    private void InitRound(ref SystemState state, Entity entity, RefRW<RoundComponent> component)
    {
        component.ValueRW.currentPhase = RoundPhase.BuyPhase;
        component.ValueRW.currentRound++;

        //IRoundManager.OnRoundStart?.Invoke(component.ValueRW.corporationScore, component.ValueRW.nativeScore);
        Vector3 spawnPosition;
        Entity respawnEntity = new EntityQueryBuilder(Allocator.Temp).WithAll<SpawnerComponent>().Build(ref state).ToEntityArray(Allocator.Temp)[0];
        spawnPosition = state.EntityManager.GetComponentData<LocalTransform>(respawnEntity).Position;

        foreach (var (health, transform) in SystemAPI.Query<RefRW<CurrentHealthComponent>, RefRW<LocalTransform>>())
        {
            transform.ValueRW.Position = spawnPosition;
            health.ValueRW.Value = 100;
        }
    }

    private void CollectorPlanted(ref SystemState state, Entity entity, RefRW<RoundComponent> component)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

        //Set the round into Post Plant
        component.ValueRW.currentPhase = RoundPhase.PostPlantPhase;
        component.ValueRW.timer = buffer[(int)RoundPhase.PostPlantPhase];

        //Make sure to delete the tag so it doesn't get detected twice
        state.EntityManager.RemoveComponent<RoundCollectorPlantedComponent>(entity);

        SendCurrentPhase(ref state, entity, component);

        state.EntityManager.AddComponent<CollectorPlantedComponent>(entity);
        //IRoundManager.OnCollectorPlanted?.Invoke();
    }

    private void SendCurrentPhase(ref SystemState state, Entity entity, RefRW<RoundComponent> component)
    {
        ChangePhaseRpcCommand rpc = new() { phase = component.ValueRW.currentPhase };
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<InitializedClient>().Build(ref state);
        EntityManager entityManager = state.WorldUnmanaged.EntityManager;
        foreach (var client in query.ToEntityArray(Allocator.Temp))
        {
            Entity rpcEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest() { TargetConnection = client });
            entityManager.AddComponentData(rpcEntity, rpc);
        }
    }

    public void OnStopRunning(ref SystemState state)
    {
    }

    
}