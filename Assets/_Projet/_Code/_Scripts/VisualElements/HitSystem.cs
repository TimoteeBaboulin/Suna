using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public struct DestroyTag : IComponentData { }

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class HitSystem : SystemBase
{
    NetcodePrefabsConverter prefabConverter;

    protected override void OnCreate()
    {
        RequireForUpdate<HitCommand>();
        RequireForUpdate<NetworkId>();

    }

    protected override void OnUpdate()
    {
        // Create a command buffer to queue structural changes
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<HitCommand>>().WithEntityAccess())
        {
            if (SystemAPI.TryGetSingleton(out VisualEffetPrefabData prefabManager))
            {
                if (prefabManager.hitVisualEffect == null) { return; }

                // Calculate the hit position
                float3 hitPosition = command.ValueRO.position + command.ValueRO.normal * 0.1f;

                // Instantiate the visual effect entity
                Entity hitEffect = commandBuffer.Instantiate(prefabManager.hitVisualEffect);

                // Add components to the hit effect
                commandBuffer.SetComponent(hitEffect, new LocalTransform
                {
                    Position = hitPosition,
                    Rotation = quaternion.identity,
                    Scale = 1.0f
                });

                // Add a DestroyTag component to the hit effect
                commandBuffer.AddComponent<DestroyTag>(hitEffect);

                // Destroy the original entity (request entity)
                commandBuffer.DestroyEntity(entity);
            }
        }

        // Playback the command buffer after the loop to apply changes
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
}

//prefabConverter = GameObject.FindFirstObjectByType<NetcodePrefabsConverter>();
//float3 hitPosition = command.ValueRO.position + command.ValueRO.normal * 0.1f;
//GameObject.Instantiate(prefabConverter.hitPrefab, new Vector3(hitPosition.x, hitPosition.y, hitPosition.z), Quaternion.identity);
//commandBuffer.DestroyEntity(entity);