using GameNetwork.Utils;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Services.Multiplayer;
using UnityEngine;
using static RoundSystemClient;



[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct RoundSystemServer : ISystem
{
    public struct NewRoundTag : IComponentData
    {

    }

    public struct VictoryRpcCommand : IRpcCommand
    {
        public TeamSideType team;
    }

    public struct ChangePhaseRpcCommand : IRpcCommand
    {
        public RoundPhase phase;
        public bool nextRound;
    }

    public struct UpdateRoundDataRpcCommand : IRpcCommand
    {
        public RoundComponent roundData;
    }

    //private EntityQuery _query;
    private bool _firstFrame;

    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        //state.RequireForUpdate<ServerDataComponent>();

        //Create the query and store it for future use
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<RoundComponent>();
        EntityQuery query = builder.Build(ref state);

        //Get the necessary references to set up the start of the game
        if (query.TryGetSingletonEntity<RoundComponent>(out var entity))
        {
            RefRW<RoundComponent> component = query.GetSingletonRW<RoundComponent>();

            InitGame(ref state, entity, component, ecb);
            _firstFrame = true;
        }
        else
        {
            _firstFrame = false;
        }
    }

    //[BurstCompile]Pas avec les RPC des sons :(
    public void OnUpdate(ref SystemState state)
    {
        //Check if the singleton exists to avoid crashes
        if (!SystemAPI.TryGetSingletonRW<RoundComponent>(out var roundComponent))
        {
            return;
        }

        if (roundComponent.ValueRO.gameWon)
            return;

        //Prepare the Entity Command Buffer to avoid breaking the reference to the component
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        Entity entity = SystemAPI.GetSingletonEntity<RoundComponent>();

        if (!_firstFrame)
        {

            InitGame(ref state, entity, roundComponent, ecb);
            _firstFrame = true;
        }

        //Debug.Log(entity);

        EntityQuery plantedHarvesterQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<HarvesterComponent, HarvesterPlanted>().Build(ref state);
        NativeArray<Entity> plantedHarvesterEntities = plantedHarvesterQuery.ToEntityArray(Allocator.Temp);

        //Check if the bomb was planted
        //Entity entity = _query.GetSingletonEntity();
        if (roundComponent.ValueRO.currentPhase is RoundPhase.ActionPhase && plantedHarvesterEntities.Length is not 0)
        {
            //Debug.Log("Collector has been planted on the server");
            CollectorPlanted(ref state, entity, roundComponent, ecb);
            //foreach (Entity harvesterEntity in plantedHarvesterEntities)
            //{
            //    ecb.SetComponentEnabled<HarvesterPlanted>(harvesterEntity, false);
            //}
        }
        else if (roundComponent.ValueRO.currentPhase is RoundPhase.PostPlantPhase && plantedHarvesterEntities.Length is 0)
        {
            HarvesterDefused(ref state, entity, roundComponent, ecb);

        }
        else
        {
            PlayerAliveCounts playerCount = SystemAPI.GetComponent<PlayerAliveCounts>(entity);

            var timeBuffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

            //Update the timer and change to next phase in case the timer runs out
            switch (roundComponent.ValueRO.currentPhase)
            {
                case RoundPhase.ActionPhase:
                    if (playerCount.nativePlayersAlive == 0)
                    {
                        Victory(ref state, entity, roundComponent, TeamSideType.Corpo, ecb);
                        roundComponent.ValueRW.currentPhase = RoundPhase.PostRoundPhase;
                        roundComponent.ValueRW.timer = timeBuffer[(int)RoundPhase.PostRoundPhase];
                        SendCurrentPhase(ref state, entity, roundComponent, ecb);
                    }
                    else if (playerCount.corpoPlayersAlive == 0)
                    {
                        Victory(ref state, entity, roundComponent, TeamSideType.Natif, ecb);
                        roundComponent.ValueRW.currentPhase = RoundPhase.PostRoundPhase;
                        roundComponent.ValueRW.timer = timeBuffer[(int)RoundPhase.PostRoundPhase];
                        SendCurrentPhase(ref state, entity, roundComponent, ecb);
                    }
                    break;
                case RoundPhase.PostPlantPhase:
                    if (playerCount.nativePlayersAlive == 0)
                    {
                        Victory(ref state, entity, roundComponent, TeamSideType.Corpo, ecb);
                        roundComponent.ValueRW.currentPhase = RoundPhase.PostRoundPhase;
                        roundComponent.ValueRW.timer = timeBuffer[(int)RoundPhase.PostRoundPhase];
                        SendCurrentPhase(ref state, entity, roundComponent, ecb);
                    }
                    break;
            }

            roundComponent.ValueRW.timer -= SystemAPI.Time.DeltaTime;

            if (roundComponent.ValueRO.timer < 0)
            {
                TimeOutPhase(ref state, entity, roundComponent, ecb);
            }

            if (roundComponent.ValueRO.timer <= 10f && roundComponent.ValueRO.currentPhase == RoundPhase.PostPlantPhase)
            {
                //Debug.Log("10 seconds left");
                SoundUtils.PlayWithRPC("Music", "Harvester_Clockwise", float3.zero);
            }
        }

        //If a client need to synchronise round data, this is used to reply with the full data
        foreach (var (request, command, rpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<RequestRoundDataRpcCommand>>().WithEntityAccess())
        {
            Entity responseEntity = ecb.CreateEntity();
            ecb.AddComponent(responseEntity, new SendRpcCommandRequest
            {
                TargetConnection = request.ValueRO.SourceConnection
            });
            ecb.AddComponent(responseEntity, new UpdateRoundDataRpcCommand
            {
                roundData = roundComponent.ValueRO
            });

            ecb.DestroyEntity(rpcEntity);
        }

        //Play the buffer back to remove/add entities as needed
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        //Debug: Allow to fake a collector plant
    }

    private void Victory(ref SystemState state, Entity entity, RefRW<RoundComponent> component, TeamSideType team, EntityCommandBuffer ecb)
    {
        //Update the score and lossstreak of the correct teams
        if (team == TeamSideType.Corpo)
        {
            component.ValueRW.corporationScore++;
            component.ValueRW.corporationLossStreak = 0;

            if (component.ValueRW.nativeLossStreak < component.ValueRW.maxStreakCount)
                component.ValueRW.nativeLossStreak++;

            SoundUtils.PlayWithRPC("Music", "Corpo", float3.zero);
        }
        else if (team == TeamSideType.Natif)
        {
            component.ValueRW.nativeScore++;
            component.ValueRW.nativeLossStreak = 0;

            if (component.ValueRW.corporationLossStreak < component.ValueRW.maxStreakCount)
                component.ValueRW.corporationLossStreak++;

            SoundUtils.PlayWithRPC("Music", "Natif", float3.zero);
        }

        foreach ((CharacterMoney moneyComponent, ClientComponent client, Entity clientEntity) in
            SystemAPI.Query<CharacterMoney, ClientComponent>()
            .WithEntityAccess())
        {
            CharacterMoney updatedMoneyComponent = moneyComponent;

            if (client.team == team)
            {
                updatedMoneyComponent.money += (uint)component.ValueRO.victoryCredits;

                if (updatedMoneyComponent.money > updatedMoneyComponent.maxMoney)
                    updatedMoneyComponent.money = updatedMoneyComponent.maxMoney;
            }
            else
            {
                uint lossStreakBonus = (uint)(component.ValueRO.lossStreakBonus * (client.team == TeamSideType.Corpo ? component.ValueRO.corporationLossStreak : component.ValueRO.nativeLossStreak));
                updatedMoneyComponent.money += (uint)component.ValueRO.lossCredits + lossStreakBonus;

                if (updatedMoneyComponent.money > updatedMoneyComponent.maxMoney)
                    updatedMoneyComponent.money = updatedMoneyComponent.maxMoney;
            }

            SystemAPI.SetComponent(clientEntity, updatedMoneyComponent);
        }

        foreach ((CharacterMoney moneyComponent, CharacterClientAttachedComponent clientAttached, Entity clientEntity) in
            SystemAPI.Query<CharacterMoney, CharacterClientAttachedComponent>()
            .WithEntityAccess())
        {
            CharacterMoney updatedMoneyComponent = moneyComponent;

            TeamSideType clientTeam = SystemAPI.GetComponent<ClientComponent>(clientAttached.ClientEntity).team;

            if (clientTeam == team)
            {
                updatedMoneyComponent.money += (uint)component.ValueRO.victoryCredits;

                if (updatedMoneyComponent.money > updatedMoneyComponent.maxMoney)
                    updatedMoneyComponent.money = updatedMoneyComponent.maxMoney;
            }
            else
            {
                uint lossStreakBonus = (uint)(component.ValueRO.lossStreakBonus * (clientTeam == TeamSideType.Corpo ? component.ValueRO.corporationLossStreak : component.ValueRO.nativeLossStreak));
                updatedMoneyComponent.money += (uint)component.ValueRO.lossCredits + lossStreakBonus;

                if (updatedMoneyComponent.money > updatedMoneyComponent.maxMoney)
                    updatedMoneyComponent.money = updatedMoneyComponent.maxMoney;
            }

            SystemAPI.SetComponent(clientEntity, updatedMoneyComponent);
        }

        //Send a RPC to clients so they get the updated score
        VictoryRpcCommand rpc = new() { team = team };
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<ClientComponent>().Build(ref state);

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
            Victory(ref state, entity, component, TeamSideType.Natif, ecb);
            component.ValueRW.currentPhase = RoundPhase.PostRoundPhase;
        }
        else
        {
            if (component.ValueRW.currentPhase == RoundPhase.PostPlantPhase)
                Victory(ref state, entity, component, TeamSideType.Corpo, ecb);
            if (component.ValueRO.currentPhase == RoundPhase.BuyPhase)
            {
                foreach ((RefRW<PhysicsCollider> physicsColliderRW, Entity barrierEntity) in SystemAPI
                    .Query<RefRW<PhysicsCollider>>()
                    .WithAll<SpawnBarrierComponent>()
                    .WithEntityAccess())
                {
                    physicsColliderRW.ValueRW.Value.Value.SetCollisionResponse(CollisionResponsePolicy.None);
                }
            }
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

        if (component.ValueRW.currentRound > component.ValueRO.maxRounds)
        {
            component.ValueRW.gameWon = true;
            GameOverRpcCommand command = new GameOverRpcCommand
            {
                winners = component.ValueRO.nativeScore > component.ValueRO.corporationScore ? TeamSideType.Natif : TeamSideType.Corpo
            };
            EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<ClientComponent>().Build(ref state);

            foreach (var client in query.ToEntityArray(Allocator.Temp))
            {
                RpcUtils.SendServerToClientRpc(ref command, client);
            }

            return;
        }

        ecb.AddComponent<NewRoundTag>(entity);

        //Mark every client to await a respawn
        foreach (var (client, respawnEntity) in SystemAPI.Query<RefRO<CharacterClientAttachedComponent>>().WithEntityAccess())
        {
            ecb.AddComponent<WaitForRespawnTag>(client.ValueRO.ClientEntity);
        }

        foreach (var (harvester, harvesterEntity) in SystemAPI
            .Query<HarvesterComponent>()
            .WithNone<HarvesterRespawn>()
            .WithEntityAccess())
        {
            ecb.AddComponent<HarvesterRespawn>(harvesterEntity);
        }

        foreach ((RefRW<PhysicsCollider> physicsColliderRW, Entity barrierEntity) in SystemAPI
                    .Query<RefRW<PhysicsCollider>>()
                    .WithAll<SpawnBarrierComponent>()
                    .WithEntityAccess())
        {
            physicsColliderRW.ValueRW.Value.Value.SetCollisionResponse(CollisionResponsePolicy.Collide);
        }

        //FIX (Aurelien) : Destroy all dropped weapons and equipment on the ground

        foreach (var (stuffOwner, stuffEntity) in SystemAPI
            .Query<RefRO<StuffDynamicData>>()
            .WithEntityAccess())
        {
            if(stuffOwner.ValueRO.owner == Entity.Null)
            {
                ecb.DestroyEntity(stuffEntity);
            }
        }
        
        SoundUtils.PlayWithRPC("Management", "StopAll", float3.zero);
    }

    private void HarvesterDefused(ref SystemState state, Entity entity, RefRW<RoundComponent> component, EntityCommandBuffer ecb)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

        component.ValueRW.currentPhase = RoundPhase.PostRoundPhase;
        component.ValueRW.timer = buffer[(int)RoundPhase.PostRoundPhase];

        Victory(ref state, entity, component, TeamSideType.Natif, ecb);
        SendCurrentPhase(ref state, entity, component, ecb);

        SoundUtils.PlayWithRPC("Music", "Harvester_Clockwise_Stop", float3.zero);
    }

    private void CollectorPlanted(ref SystemState state, Entity entity, RefRW<RoundComponent> component, EntityCommandBuffer ecb)
    {
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

        //Set the round into Post Plant
        component.ValueRW.currentPhase = RoundPhase.PostPlantPhase;
        component.ValueRW.timer = buffer[(int)RoundPhase.PostPlantPhase];

        //Make sure to delete the tag so it doesn't get detected twice
        ecb.RemoveComponent<RoundCollectorPlantedComponent>(entity);


        //Update the clients with the correct phase
        SendCurrentPhase(ref state, entity, component, ecb);
    }

    private void SendCurrentPhase(ref SystemState state, Entity entity, RefRW<RoundComponent> component, EntityCommandBuffer ecb)
    {
        //Send a RPC to update the phase of the clients based on the server's
        ChangePhaseRpcCommand rpc = new() { phase = component.ValueRW.currentPhase, nextRound = component.ValueRW.currentPhase == RoundPhase.BuyPhase };
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<ClientComponent>().Build(ref state);

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

    //private void UpdateRoundData(ref SystemState state, Entity target, RefRO<RoundComponent> component, EntityCommandBuffer ecb)
    //{

    //}
}
