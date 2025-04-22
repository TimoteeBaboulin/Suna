using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class SpawnerUtils
{
    [BurstCompile]
    private static bool TryGetSpawnerSettingsEntity(ref SystemState state, out Entity entity)
    {
        EntityQueryBuilder query = new EntityQueryBuilder(Allocator.Temp);

        if (query.WithAll<SpawnerSettingsTag>().Build(ref state)
            .TryGetSingletonEntity<SpawnerSettingsTag>(out Entity spawnerSettingsEntity))
        {
            entity = spawnerSettingsEntity;
            return true;
        }
        else
        {
            Debug.LogError("Unable to find the spawnerSettings singleton.");
            entity = Entity.Null;
            return false;
        }
    }

    [BurstCompile]
    private static bool SpawnerSettingsHasAuthoRespawnEnableComponent(ref SystemState state)
    {
        if (!TryGetSpawnerSettingsEntity(ref state, out Entity spawnerSettingsEntity)) return false;

        if (!state.EntityManager.HasComponent<AutoRespawnIsEnable>(spawnerSettingsEntity))
        {
            Debug.LogError("SpawnerSettings does not have an AutoRespawnIsEnable component.");
            return false;
        }

        return true;
    }

    [BurstCompile]
    public static bool AutoRespawnIsEnable(ref SystemState state)
    {
        if (!TryGetSpawnerSettingsEntity(ref state, out Entity spawnerSettingsEntity)) return false;
        if (!SpawnerSettingsHasAuthoRespawnEnableComponent(ref state)) return false;

        return state.EntityManager.IsComponentEnabled<AutoRespawnIsEnable>(spawnerSettingsEntity);
    }

    [BurstCompile]
    public static void SetAutoRespawn(bool isEnable, ref SystemState state)
    {
        if (!TryGetSpawnerSettingsEntity(ref state, out Entity spawnerSettingsEntity)) return;
        if (!SpawnerSettingsHasAuthoRespawnEnableComponent(ref state)) return;

        state.EntityManager.SetComponentEnabled<AutoRespawnIsEnable>(spawnerSettingsEntity, isEnable);
    }

    [BurstCompile]
    public static void RespawnAllClient(ref SystemState state, in EntityCommandBuffer ecb)
    {
        EntityQueryBuilder query = new EntityQueryBuilder(Allocator.Temp);

        foreach (var clientEntity in query
            .WithAll<ClientComponent>()
            .Build(ref state)
            .ToEntityArray(Allocator.Temp))
        {
            ecb.AddComponent<WaitForRespawnTag>(clientEntity);
        }
    }
}
