using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

public struct DestroyTag : IComponentData { }
public struct Lifetime : IComponentData
{
    public float RemainingTime;
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class HitSystem : SystemBase
{
    NetcodePrefabsConverter prefabConverter;

    protected override void OnCreate()
    {
        RequireForUpdate<HitCommand>();
        RequireForUpdate<NetworkId>();

    }

    struct TracerData
    {
        public float3 start;
        public float3 end;
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<HitCommand>>().WithEntityAccess())
        {
            if (SystemAPI.TryGetSingleton(out VisualEffetPrefabData prefabManager))
            {
                if (prefabManager.hitVisualEffect == null) { return; }

                float3 hitPosition = command.ValueRO.position + command.ValueRO.normal * 0.1f;
                Entity hitEffect = commandBuffer.Instantiate(prefabManager.hitVisualEffect);
                Entity tracerEntity = commandBuffer.Instantiate(prefabManager.tracerRoundVisualEffect);
                float tracerSpeed = SystemAPI.GetComponentRO<TracerRoundComponent>(prefabManager.tracerRoundVisualEffect).ValueRO.speed;
                
                commandBuffer.SetComponent(tracerEntity, new LocalTransform
                {
                    Position = command.ValueRO.origin,
                    Rotation = quaternion.identity,
                    Scale = 1.0f
                });
                commandBuffer.SetComponent(tracerEntity, new TracerRoundComponent
                {
                    start = command.ValueRO.origin,
                    end = command.ValueRO.position,
                    speed = tracerSpeed
                });
                commandBuffer.SetComponent(hitEffect, new LocalTransform
                {
                    Position = hitPosition,
                    Rotation = quaternion.identity,
                    Scale = 1.9f
                });


                commandBuffer.AddComponent<DestroyTag>(hitEffect);
                commandBuffer.AddComponent<DestroyTag>(tracerEntity);

                if (SystemAPI.TryGetSingleton(out VFXDurationData durationData))
                {
                    commandBuffer.AddComponent(hitEffect, new Lifetime { RemainingTime = durationData.hitVFXDuration });
                    commandBuffer.AddComponent(tracerEntity, new Lifetime { RemainingTime = durationData.tracerVFXDuration });
                }

                commandBuffer.DestroyEntity(entity);

            }
            commandBuffer.DestroyEntity(entity);
        }
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
}