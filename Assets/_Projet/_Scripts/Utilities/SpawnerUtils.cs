
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class SpawnerUtils
{
    private static bool TryGetSpawnerSettingsEntity(out Entity entity)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnerSettingsTag>());

        if (query.TryGetSingletonEntity<SpawnerSettingsTag>(out Entity spawnerSettingsEntity))
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

    private static bool SpawnerSettingsHasAuthoRespawnEnableComponent()
    {
        if (!TryGetSpawnerSettingsEntity(out Entity spawnerSettingsEntity)) return false;

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnerSettingsTag>());

        if (!entityManager.HasComponent<AutoRespawnIsEnable>(spawnerSettingsEntity))
        {
            Debug.LogError("SpawnerSettings does not have an AutoRespawnIsEnable component.");
            return false;
        }

        return true;
    }

    public static bool AutoRespawnIsEnable()
    {
        if (!TryGetSpawnerSettingsEntity(out Entity spawnerSettingsEntity)) return false;
        if (!SpawnerSettingsHasAuthoRespawnEnableComponent()) return false;

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnerSettingsTag>());

        return entityManager.IsComponentEnabled<AutoRespawnIsEnable>(spawnerSettingsEntity);
    }

    public static void SetAutoRespawn(bool isEnable)
    {
        if (!TryGetSpawnerSettingsEntity(out Entity spawnerSettingsEntity)) return;
        if (!SpawnerSettingsHasAuthoRespawnEnableComponent()) return;

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnerSettingsTag>());

        entityManager.SetComponentEnabled<AutoRespawnIsEnable>(spawnerSettingsEntity, isEnable);
    }

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
