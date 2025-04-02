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
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ShopCommand>>().WithEntityAccess())
        {
            if (SystemAPI.TryGetSingletonBuffer<GameResourcesInstanciateStuffQueu>(out var queue))
            {
                queue.Add(new GameResourcesInstanciateStuffQueu
                {
                    StuffName = command.ValueRO.weaponData,
                    Owner = request.ValueRO.SourceConnection
                });
            }

            commandBuffer.DestroyEntity(entity);
        }
        
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
}
