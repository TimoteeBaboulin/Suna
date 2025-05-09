using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial class ShopSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ShopCommand>();
        RequireForUpdate<NetworkId>();
        RequireForUpdate<GameResourcesDatabase>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
        
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ShopCommand>>().WithEntityAccess())
        {
            bool canBuy = true;
            if (SystemAPI.TryGetSingleton<RoundComponent>(out var roundComponent))
            {
                if (roundComponent.currentPhase != RoundPhase.BuyPhase && roundComponent.roundSystemActive)
                    canBuy = false;
            }

            if (canBuy)
            {
                RefRO<NetworkId> requestNetworkId = SystemAPI.GetComponentRO<NetworkId>(request.ValueRO.SourceConnection);

                uint dataReceived = FindPriceByName(command.ValueRO.weaponData.ToString());

                foreach (var (characterAttached, ghostOwner) in SystemAPI.Query<RefRO<ClientCharacterAttached>, RefRO<GhostOwner>>())
                {
                    if (ghostOwner.ValueRO.NetworkId != requestNetworkId.ValueRO.Value)
                    {
                        continue;
                    }

                    Entity character = characterAttached.ValueRO.Value;
                    CharacterMoney money = SystemAPI.GetComponent<CharacterMoney>(character);

                    if (SystemAPI.TryGetSingletonBuffer<InstantiateStuffQueue>(out var queue) && money.money >= dataReceived)
                    {
                        StuffUtils.InstantiateNextFrame(queue, command.ValueRO.weaponData, character);

                        money.money -= dataReceived;
                        commandBuffer.SetComponent(character, money);

                        break;
                    }
                }
            }
            
            commandBuffer.DestroyEntity(entity);
        }
        
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }

    private uint FindPriceByName(string name)
    {
        foreach(var database in SystemAPI.Query<RefRO<GameResourcesDatabase>>())
        {
            ref var db = ref database.ValueRO.StuffDatabaseRef.Value.StuffCommonData;

            for(int i = 0; i < db.Length; i++)
            {
                if (db[i].Name.ToString() == name)
                {
                    return (uint)db[i].price;
                }
            }
        }

        return 0;
    }
}