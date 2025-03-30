using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class HitSystem : SystemBase
{
    NetcodePrefabsConverter prefabConverter;

    protected override void OnCreate()
    {
        RequireForUpdate<HitCommand>();
        RequireForUpdate<NetworkId>();

        prefabConverter = GameObject.FindFirstObjectByType<NetcodePrefabsConverter>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<HitCommand>>().WithEntityAccess())
        {
            //if (SystemAPI.TryGetSingleton(out VisualEffetPrefabData prefabManager))
            //{
            //    if (prefabManager.hitVisualEffect == null) { return; }

            //    float3 hitPosition = command.ValueRO.position + command.ValueRO.normal * 0.1f;

            //    Entity hitEffect = commandBuffer.Instantiate(prefabManager.hitVisualEffect);

            //    commandBuffer.AddComponent(hitEffect, new LocalTransform
            //    {
            //        Position = hitPosition,
            //        Rotation = Quaternion.identity,
            //        Scale = 1.0f
            //    });

            //    commandBuffer.AddComponent<DestroyTag>(hitEffect);
            //    commandBuffer.DestroyEntity(entity);
            //}

            if (prefabConverter.hitPrefab != null)
            {
                float3 hitPosition = command.ValueRO.position + command.ValueRO.normal * 0.1f;
                GameObject.Instantiate(prefabConverter.hitPrefab, new Vector3(hitPosition.x, hitPosition.y, hitPosition.z), Quaternion.identity);
                commandBuffer.DestroyEntity(entity);
            }
        }
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
}