using GameNetwork.Utils;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;
using static RoundSystemServer;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct RoundSystemClient : ISystem
{
    private bool _running;

    private bool _firstFrame;

    public struct RequestRoundDataRpcCommand : IRpcCommand
    {
    }

    public void OnCreate(ref SystemState state)
    {
        _running = true;
        _firstFrame = true;

        state.RequireForUpdate<NetworkTime>();

        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<RoundComponent>();
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

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!_running) return;

        //Get the Read/Write Reference of the component for use in functions
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<RoundComponent>().Build(ref state);
        RefRW<RoundComponent> round;
        if (!query.TryGetSingletonRW(out round))
            return;

        EntityCommandBuffer buffer = new EntityCommandBuffer(Allocator.Temp);

        if (_firstFrame)
        {
            EntityQuery networkQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<NetworkStreamConnection>().Build(ref state);
            if (networkQuery.TryGetSingleton<NetworkStreamConnection>(out var streamConnection))
            {
                if (streamConnection.CurrentState != ConnectionState.State.Connected)
                    return;

                RequestUpdate(ref state, buffer);
            }
            else
            {
                return;
            }
            
        }

        
        _firstFrame = false;
        //Prepare the buffer to use at the end of the update to avoid breaking the reference to the component

        //Basic timer tick
        round.ValueRW.timer -= Time.deltaTime;
        if (round.ValueRW.timer < 0)
        {
            round.ValueRW.timer = 0;
        }

        var b = SystemAPI.GetBuffer<PhaseTimesBuffer>(query.GetSingletonEntity());

        if (round.ValueRW.currentPhase == RoundPhase.ActionPhase && (b[(int)RoundPhase.ActionPhase] - round.ValueRW.timer) < 2)
        {
            foreach (var (matOverride, entity) in SystemAPI.Query<RefRW<SpawnFenceMaterialOverride>>().WithEntityAccess())
            {
                matOverride.ValueRW.Value = (b[(int)RoundPhase.ActionPhase] - round.ValueRW.timer) / 2.0f;
            }
        }

        foreach(var (request, update, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<UpdateRoundDataRpcCommand>>().WithEntityAccess())
        {
            round.ValueRW = update.ValueRO.roundData;
            buffer.DestroyEntity(entity);
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
            ChangePhase(ref state, newRoundComponent.ValueRO.phase, query.GetSingletonEntity(), round, buffer);
            buffer.DestroyEntity(entity);
        }

        foreach (var (rpcComponent, gameOverRpc, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GameOverRpcCommand>>().WithEntityAccess())
        {
            round.ValueRW.gameWon = true;
            round.ValueRW.winners = gameOverRpc.ValueRO.winners;

            //TODO: Don't disconnect instantly, let a win/lose screen appear first
            LoadUtils.QuitAsync();
            SceneManager.LoadSceneAsync(0);
            buffer.DestroyEntity(entity);
        }

        buffer.Playback(state.EntityManager);
        buffer.Dispose();
    }

    public void RequestUpdate(ref SystemState state, EntityCommandBuffer ecb)
    {
        RequestRoundDataRpcCommand rpc = new() {};

        Entity newEntity = ecb.CreateEntity();
        ecb.AddComponent(newEntity, rpc);
        ecb.AddComponent(newEntity, new SendRpcCommandRequest());
    }

    public void ChangeScore(ref SystemState state, TeamSideType team, RefRW<RoundComponent> component) {
        //Update the score and loss streak of the corresponding teams
        switch (team)
        {
            case TeamSideType.Corpo:
                component.ValueRW.corporationScore++;
                component.ValueRW.nativeLossStreak = System.Math.Min(component.ValueRW.nativeLossStreak + 1, component.ValueRW.maxStreakCount);
                component.ValueRW.corporationLossStreak = 0;

                break;

            case TeamSideType.Natif:
                component.ValueRW.nativeScore++;
                component.ValueRW.corporationLossStreak = System.Math.Min(component.ValueRW.corporationLossStreak + 1, component.ValueRW.maxStreakCount);
                component.ValueRW.nativeLossStreak = 0;

                break;
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
    }
    public void ChangePhase(ref SystemState state, RoundPhase phase, Entity entity, RefRW<RoundComponent> component, EntityCommandBuffer ecb)
    {
        //Update the timer and phases after receiving an update
        var buffer = SystemAPI.GetBuffer<PhaseTimesBuffer>(entity);

        component.ValueRW.currentPhase = phase;
        component.ValueRW.timer = buffer[(int)phase];

        if (phase != RoundPhase.BuyPhase)
        {
            foreach ((RefRW<PhysicsCollider> physicsColliderRW, RefRW <SpawnFenceMaterialOverride> matOverride, Entity barrierEntity) in SystemAPI
                    .Query<RefRW<PhysicsCollider>, RefRW<SpawnFenceMaterialOverride>>()
                    .WithAll<SpawnBarrierComponent>()
                    .WithEntityAccess())
            {
                physicsColliderRW.ValueRW.Value.Value.SetCollisionResponse(CollisionResponsePolicy.None);

                matOverride.ValueRW.Value = 1;
            }
        }
        else
        {
            foreach ((RefRW<PhysicsCollider> physicsColliderRW, RefRW<SpawnFenceMaterialOverride> matOverride, Entity barrierEntity) in SystemAPI
                    .Query<RefRW<PhysicsCollider>, RefRW<SpawnFenceMaterialOverride>>()
                    .WithAll<SpawnBarrierComponent>()
                    .WithEntityAccess())
            {
                physicsColliderRW.ValueRW.Value.Value.SetCollisionResponse(CollisionResponsePolicy.Collide);

                matOverride.ValueRW.Value = 0;
            }
        }
    }
}
