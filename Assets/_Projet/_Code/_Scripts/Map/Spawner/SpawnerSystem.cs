using Unity.Entities;
using Unity.Transforms;

public struct WaitingForRespawnTag : IComponentData { }

public struct RespawnZones : IBufferElementData
{
    public Entity entity; // Référence à une entité représentant une zone de respawn
}
public struct Player : IComponentData
{
    public Entity teamEntity; // Référence à l'entité de l'équipe du joueur
    public TeamSideType side;
    public int hp;
}

public partial struct WaitingForRespawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        foreach (var (playerRef, entity) in SystemAPI.Query<RefRO<Player>>()
            .WithEntityAccess())
        {
            if (entityManager.HasComponent<WaitingForRespawnTag>(entity)) continue;

            ref readonly Player player = ref playerRef.ValueRO;

            if (player.hp <= 0)
            {
                entityManager.AddComponent<WaitingForRespawnTag>(entity);
            }
        }
    }
}

public partial struct SpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityManager entityManager = state.EntityManager;

        foreach (var (playerRef, playerTransformRef, entity) in SystemAPI.Query<RefRO<Player>, RefRW<LocalTransform>>()
            .WithAll<WaitingForRespawnTag>()
            .WithEntityAccess())
        {
            ref readonly Player player = ref playerRef.ValueRO;
            ref LocalTransform playerTransform = ref playerTransformRef.ValueRW;

            if (entityManager.HasComponent<RespawnZones>(player.teamEntity))
            {
                DynamicBuffer<RespawnZones> respawnZonesBuffer = entityManager.GetBuffer<RespawnZones>(player.teamEntity);

                if (respawnZonesBuffer.Length > 0)
                {
                    Entity respawnZoneEntity = respawnZonesBuffer[0].entity;

                    if (entityManager.HasComponent<LocalTransform>(respawnZoneEntity))
                    {
                        LocalTransform respawnZoneTransform = entityManager.GetComponentData<LocalTransform>(respawnZoneEntity);

                        playerTransform.Position = respawnZoneTransform.Position;
                    }
                    else 
                        UnityEngine.Debug.LogWarning($"Respawn zone entity {respawnZoneEntity} does not have a LocalTransform component.");
                }
                else 
                    UnityEngine.Debug.LogWarning($"No respawn zones found for team entity {player.teamEntity}.");
            }
            else 
                UnityEngine.Debug.LogWarning($"Team entity {player.teamEntity} does not have a RespawnZones component.");
        }
    }
}

