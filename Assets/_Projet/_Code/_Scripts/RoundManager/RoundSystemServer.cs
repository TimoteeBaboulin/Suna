using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

public partial struct RoundSystemServer : ISystem, ISystemStartStop, IRoundManager
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

        //Get the necessary references to set up the start of the game
        var entity = _query.GetSingletonEntity();
        RoundComponent component = _query.GetSingleton<RoundComponent>();

        InitGame(ref state, entity, ref component);
        IRoundManager._currentTime = component.timer;

        //Need to write the changed values back onto the component
        SystemAPI.SetSingleton(component);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!_running) return;

        //Check if the singleton exists to avoid crashes
        if (!SystemAPI.TryGetSingleton<RoundComponent>(out var roundComponent))
        {
            throw new System.Exception("Couldn't find RoundComponent Singleton, please check that there is a single RoundManager in the world.");
        }

        //Check if the bomb was planted
        Entity entity = _query.GetSingletonEntity();
        if (roundComponent.currentPhase == RoundPhase.ActionPhase && state.EntityManager.HasComponent<RoundCollectorPlantedComponent>(entity))
        {
            CollectorPlanted(ref state, entity, ref roundComponent);
        }
        else
        {
            //Update the timer and change to next phase in case the timer runs out
            roundComponent.timer -= Time.deltaTime;
            if (roundComponent.timer < 0)
            {
                TimeOutPhase(ref state, entity, ref roundComponent);
            }
        }

        IRoundManager._currentTime = roundComponent.timer;

        //Write the values in memory
        SystemAPI.SetSingleton(roundComponent);

        if (Keyboard.current[Key.Space].wasPressedThisFrame)
        {
            state.EntityManager.AddComponent<RoundCollectorPlantedComponent>(entity);
        }
    }

    private void Victory(ref SystemState state, Entity entity, ref RoundComponent component, TimoteeTeam team)
    {
        if (team == TimoteeTeam.Corporation)
        {
            component.corporationScore++;
            component.corporationLossStreak = 0;

            if (component.nativeLossStreak < component.maxStreakCount)
                component.nativeLossStreak++;
        }
        else
        {
            component.nativeScore++;
            component.nativeLossStreak = 0;

            if (component.corporationLossStreak < component.maxStreakCount)
                component.corporationLossStreak++;
        }

        ServerSystem system = state.World.GetOrCreateSystemManaged<ServerSystem>();
        if (system == null)
        {
            Debug.Log("Couldn't find server system reference.");
            return;
        }

        VictoryRpcCommand rpc = new() { team = team };
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<InitializedClient>().Build(ref state);
        foreach (var client in query.ToEntityArray(Allocator.Temp))
        {
            system.SendMessageRpc("message", ConnectionManager.Instance.Server, ref rpc, client);
        }
    }

    private void TimeOutPhase(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        //Gets to next phase because of time out
        if (component.currentPhase == RoundPhase.ActionPhase)
        {
            Victory(ref state, entity, ref component, TimoteeTeam.Natives);
            component.currentPhase = RoundPhase.PostRoundPhase; 
        }
        else
        {
            if (component.currentPhase == RoundPhase.PostPlantPhase)
                Victory(ref state, entity, ref component, TimoteeTeam.Corporation);
            component.currentPhase++;
        }

        //If the round ended, get to next one
        if (component.currentPhase > RoundPhase.PostRoundPhase)
        {
            InitRound(ref state, entity, ref component);
        }

        //Sets the timer for the new phase
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);
        component.timer = buffer[(int)component.currentPhase];

        SendCurrentPhase(ref state, entity, ref component);
    }

    private void InitGame(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);
        component.currentPhase = RoundPhase.BuyPhase;
        component.timer = buffer[(int)component.currentPhase];
        component.currentRound = 0;
        component.nativeScore = 0;
        component.corporationScore = 0;
        InitRound(ref state, entity, ref component);
    }

    private void InitRound(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        component.currentPhase = RoundPhase.BuyPhase;
        component.currentRound++;

        IRoundManager.OnRoundStart?.Invoke(component.corporationScore, component.nativeScore);
        Vector3 spawnPosition;
        Entity respawnEntity = new EntityQueryBuilder(Allocator.Temp).WithAll<SpawnerComponent>().Build(ref state).ToEntityArray(Allocator.Temp)[0];
        spawnPosition = state.EntityManager.GetComponentData<LocalTransform>(respawnEntity).Position;

        foreach (var (health, transform) in SystemAPI.Query<RefRW<CurrentHealthComponent>, RefRW<LocalTransform>>())
        {
            transform.ValueRW.Position = spawnPosition;
            health.ValueRW.Value = 100;
        }
    }

    private void CollectorPlanted(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

        //Set the round into Post Plant
        component.currentPhase = RoundPhase.PostPlantPhase;
        component.timer = buffer[(int)RoundPhase.PostPlantPhase];

        //Make sure to delete the tag so it doesn't get detected twice
        state.EntityManager.RemoveComponent<RoundCollectorPlantedComponent>(entity);

        SendCurrentPhase(ref state, entity, ref component);

        IRoundManager.OnCollectorPlanted?.Invoke();
    }

    private void SendCurrentPhase(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        ServerSystem system = state.World.GetOrCreateSystemManaged<ServerSystem>();
        if (system == null)
        {
            Debug.Log("Couldn't find server system reference.");
            return;
        }

        ChangePhaseRpcCommand rpc = new() { phase = component.currentPhase};
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<InitializedClient>().Build(ref state);
        foreach (var client in query.ToEntityArray(Allocator.Temp))
        {
            system.SendMessageRpc("message", ConnectionManager.Instance.Server, ref rpc, client);
        }
    }

    public void OnStopRunning(ref SystemState state)
    {
    }

    
}