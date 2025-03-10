using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using static RoundSystemClient;

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

    public struct UpdateRoundDataRpcCommand : IRpcCommand
    {

    }

    private EntityQuery _query;

    public enum TimoteeTeam : byte //TODO: Switch to a normalized enum for the whole project
    {
        Corporation,
        Natives
    };

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        state.RequireForUpdate<ServerDataComponent>();

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

                InitGame(ref state, entity, component, ecb);
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Check if the singleton exists to avoid crashes
        if (!SystemAPI.TryGetSingletonRW<RoundComponent>(out var roundComponent))
        {
            return;
        }

        //Prepare the Entity Command Buffer to avoid breaking the reference to the component
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

        foreach (var (request, command, rpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<RequestRoundDataRpcCommand>>().WithEntityAccess())
        {

            ecb.DestroyEntity(rpcEntity);
        }

        //Play the buffer back to remove/add entities as needed
        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        //Debug: Allow to fake a collector plant
        //if (Keyboard.current[Key.Space].wasPressedThisFrame)
        //{
        //    state.EntityManager.AddComponent<RoundCollectorPlantedComponent>(entity);
        //}
    }

    private void Victory(ref SystemState state, Entity entity, RefRW<RoundComponent> component, TimoteeTeam team, EntityCommandBuffer ecb)
    {
        //Update the score and lossstreak of the correct teams
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

        //Send a RPC to clients so they get the updated score
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
        //Update the score accordingly
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
            InitRound(ref state, entity, component, ecb);
        }

        //Sets the timer for the new phase
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);
        component.ValueRW.timer = buffer[(int)component.ValueRW.currentPhase];

        //Send the message to the clients
        SendCurrentPhase(ref state, entity, component, ecb);
    }

    private void InitGame(ref SystemState state, Entity entity, RefRW<RoundComponent> component, EntityCommandBuffer ecb)
    {
        //Initialize the Round Component with the correct data
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);
        component.ValueRW.currentPhase = RoundPhase.BuyPhase;
        component.ValueRW.timer = buffer[(int)component.ValueRW.currentPhase];
        component.ValueRW.currentRound = 0;
        component.ValueRW.nativeScore = 0;
        component.ValueRW.corporationScore = 0;
        InitRound(ref state, entity, component, ecb);
        SendCurrentPhase(ref state, entity, component, ecb);
    }

    private void InitRound(ref SystemState state, Entity entity, RefRW<RoundComponent> component, EntityCommandBuffer ecb)
    {
        //Reset the phase and increase the round number
        component.ValueRW.currentPhase = RoundPhase.BuyPhase;
        component.ValueRW.currentRound++;

        //Mark every client to await a respawn
        foreach (var (client, respawnEntity) in SystemAPI.Query<RefRO<CharacterClientAttachedComponent>>().WithEntityAccess())
        {
            ecb.AddComponent<WaitForRespawnTag>(client.ValueRO.ClientEntity);
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

        //Update the clients with the correct phase
        SendCurrentPhase(ref state, entity, component, ecb);
    }

    private void SendCurrentPhase(ref SystemState state, Entity entity, RefRW<RoundComponent> component, EntityCommandBuffer ecb)
    {
        //Send a RPC to update the phase of the clients based on the server's
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

    private void UpdateRoundData(ref SystemState state, Entity target, RefRO<RoundComponent> component, EntityCommandBuffer ecb)
    {

    }
}