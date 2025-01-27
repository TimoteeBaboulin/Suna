using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public partial struct RoundManagerServer : ISystem, ISystemStartStop
{
    private EntityQuery _query;

    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        //Create the query and store it for future use
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<RoundComponent>();
        _query = builder.Build(ref state);

        //Get the necessary references to set up the start of the game
        var entity = _query.GetSingletonEntity();
        RoundComponent component = SystemAPI.GetSingleton<RoundComponent>();

        InitGame(ref state, entity, ref component);

        //Need to write the changed values back onto the component
        SystemAPI.SetSingleton<RoundComponent>(component);
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
        if (state.EntityManager.HasComponent<RoundCollectorPlantedComponent>(entity))
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
        SystemAPI.SetSingleton<RoundComponent>(roundComponent);

        if (Keyboard.current[Key.Space].wasPressedThisFrame)
        {
            state.EntityManager.AddComponent<RoundCollectorPlantedComponent>(entity);
        }
    }

    private void TimeOutPhase(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        //Gets to next phase because of time out
        if (component.currentPhase == RoundPhase.ActionPhase)
            component.currentPhase = RoundPhase.PostRoundPhase;
        else
            component.currentPhase++;

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
    }

    private void InitRound(ref SystemState state, Entity entity, ref RoundComponent component)
    {
        component.currentPhase = RoundPhase.BuyPhase;
        component.currentRound++;

        Debug.Log("Passing to round number " +  component.currentRound);
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