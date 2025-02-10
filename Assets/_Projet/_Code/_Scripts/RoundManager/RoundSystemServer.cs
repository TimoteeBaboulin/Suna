using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct RoundSystemServer : ISystem
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
    public void OnCreate(ref SystemState state)
    {
        //TODO: Switch to a RequireForUpdate to avoid performance drops
        //if (state.World != ConnectionManager.Instance.Server)
        //{
        //    _running = false;
        //    return;
        //}

        _running = true;

        //Create the query and store it for future use
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<RoundComponent>();
        _query = builder.Build(ref state);

        //Get the necessary references to set up the start of the game

        //var entity = _query.GetSingletonEntity();
        if (_query.TryGetSingletonEntity<Entity>(out var entity))
        {
            if (state.EntityManager.Exists(entity))
            {
                RefRW<RoundComponent> component = _query.GetSingletonRW<RoundComponent>();

                InitGame(ref state, entity, component);
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!_running) return;

        //Check if the singleton exists to avoid crashes
        if (!SystemAPI.TryGetSingletonRW<RoundComponent>(out var roundComponent))
        {
            //throw new System.Exception("Couldn't find RoundComponent Singleton, please check that there is a single RoundManager in the world.");
            return;
        }

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        //Check if the bomb was planted
        Entity entity = _query.GetSingletonEntity();
        if (roundComponent.ValueRO.currentPhase == RoundPhase.ActionPhase && state.EntityManager.HasComponent<RoundCollectorPlantedComponent>(entity))
        {
            CollectorPlanted(ref state, entity, roundComponent, ecb);
        }
        else
        {
            //Update the timer and change to next phase in case the timer runs out
            roundComponent.ValueRW.timer -= Time.deltaTime;
            if (roundComponent.ValueRO.timer < 0)
            {
                TimeOutPhase(ref state, entity, roundComponent, ecb);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        //if (Keyboard.current[Key.Space].wasPressedThisFrame)
        //{
        //    state.EntityManager.AddComponent<RoundCollectorPlantedComponent>(entity);
        //}
    }

    private void Victory(ref SystemState state, Entity entity, RefRW<RoundComponent> component, TimoteeTeam team, EntityCommandBuffer ecb)
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

        foreach (var client in query.ToEntityArray(Allocator.Temp))
        {

            Entity newEntity = ecb.CreateEntity();
            ecb.AddComponent(newEntity, rpc);
            ecb.AddComponent(newEntity, new SendRpcCommandRequest()
            {
                TargetConnection = client
            });
        }
    }

    private void TimeOutPhase(ref SystemState state, Entity entity, RefRW<RoundComponent> component, EntityCommandBuffer ecb)
    {
        //Gets to next phase because of time out
        if (component.ValueRW.currentPhase == RoundPhase.ActionPhase)
        {
            Victory(ref state, entity, component, TimoteeTeam.Natives, ecb);
            component.ValueRW.currentPhase = RoundPhase.PostRoundPhase;
        }
        else
        {
            if (component.ValueRW.currentPhase == RoundPhase.PostPlantPhase)
                Victory(ref state, entity, component, TimoteeTeam.Corporation, ecb);
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

        SendCurrentPhase(ref state, entity, component, ecb);
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

        Vector3 spawnPosition;
        Entity respawnEntity = new EntityQueryBuilder(Allocator.Temp).WithAll<SpawnerComponent>().Build(ref state).ToEntityArray(Allocator.Temp)[0];
        spawnPosition = state.EntityManager.GetComponentData<LocalTransform>(respawnEntity).Position;

        foreach (var (health, transform) in SystemAPI.Query<RefRW<CurrentHealthComponent>, RefRW<LocalTransform>>())
        {
            transform.ValueRW.Position = spawnPosition;
            health.ValueRW.Value = 100;
        }
    }

    private void CollectorPlanted(ref SystemState state, Entity entity, RefRW<RoundComponent> component, EntityCommandBuffer ecb)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

        //Set the round into Post Plant
        component.ValueRW.currentPhase = RoundPhase.PostPlantPhase;
        component.ValueRW.timer = buffer[(int)RoundPhase.PostPlantPhase];

        //Make sure to delete the tag so it doesn't get detected twice
        state.EntityManager.RemoveComponent<RoundCollectorPlantedComponent>(entity);

        SendCurrentPhase(ref state, entity, component, ecb);
    }

    private void SendCurrentPhase(ref SystemState state, Entity entity, RefRW<RoundComponent> component, EntityCommandBuffer ecb)
    {
        ChangePhaseRpcCommand rpc = new() { phase = component.ValueRW.currentPhase };
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<InitializedClient>().Build(ref state);



        foreach (var client in query.ToEntityArray(Allocator.Temp))
        {
            Entity newEntity = ecb.CreateEntity();
            ecb.AddComponent(newEntity, rpc);
            ecb.AddComponent(newEntity, new SendRpcCommandRequest()
            {
                TargetConnection = client
            });
        }
    }



}