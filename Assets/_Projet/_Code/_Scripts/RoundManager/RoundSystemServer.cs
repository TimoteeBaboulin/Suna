using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public partial struct RoundSystemServer : ISystem, ISystemStartStop, IRoundManager
{
    private EntityQuery _query;
    private enum TimoteeTeam
    {
        Corporation,
        Natives
    };

    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<ServerComponent>();
        state.RequireForUpdate(state.GetEntityQuery(builder));

        //Create the query and store it for future use
        builder = new EntityQueryBuilder(Allocator.Temp).WithAll<RoundComponent>();
        _query = builder.Build(ref state);

        //Get the necessary references to set up the start of the game
        var entity = _query.GetSingletonEntity();
        RoundComponent component = SystemAPI.GetSingleton<RoundComponent>();

        InitGame(ref state, entity, ref component);

        //Need to write the changed values back onto the component
        SystemAPI.SetSingleton(component);
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
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
    }

    private void InitGame(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);
        component.currentPhase = RoundPhase.BuyPhase;
        component.timer = buffer[(int)component.currentPhase];
        component.currentRound = 1;
        component.nativeScore = 0;
        component.corporationScore = 0;
    }

    private void InitRound(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        component.currentPhase = RoundPhase.BuyPhase;
        component.currentRound++;

        Debug.Log("Passing to round number " +  component.currentRound);
        ServerSystem system = state.World.GetOrCreateSystemManaged<ServerSystem>();
        if (system == null)
        {
            Debug.Log("Couldn't find server system reference.");
            return;
        }

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<InitializedClient>().Build(ref state);
        foreach (var client in query.ToEntityArray(Allocator.Temp))
        {
            //system.SendMessageRpc("Init Round", ConnectionManager.Instance.Server, client);
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
    }

    public void OnStopRunning(ref SystemState state)
    {
    }
}