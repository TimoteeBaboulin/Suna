using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public struct WaitingForRespawnTag : IComponentData { }

public struct RespawnPoints : IBufferElementData
{
    public Entity entity; // Référence à une entité représentant une zone de respawn
}
public struct Player : IComponentData
{
    public Entity teamEntity; // Référence à l'entité de l'équipe du joueur
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

        //On recupére les joueurs qui sont en attente d'un respawn
        foreach (var (playerRef, playerTransformRef, entity) in SystemAPI.Query<RefRO<Player>, RefRW<LocalTransform>>()
            .WithAll<WaitingForRespawnTag>()
            .WithEntityAccess())
        {
            //Simplification des appels
            ref readonly Player player = ref playerRef.ValueRO;
            ref LocalTransform playerTransform = ref playerTransformRef.ValueRW;

            //On verifie si des entitées avec le composant "RespawnPoint" exist
            if (entityManager.HasComponent<RespawnPoints>(player.teamEntity))
            {
                //On récupére la liste des points de respawn
                DynamicBuffer<RespawnPoints> respawnZonesBuffer = entityManager.GetBuffer<RespawnPoints>(player.teamEntity);

                //On verifie que le nombre de point de respawn est > 0
                if (respawnZonesBuffer.Length > 0)
                {
                    //On recupere le premier point de spawn de la liste 
                    Entity respawnZoneEntity = respawnZonesBuffer[0].entity;

                    //On verifie que le point de respawn a bien un transform
                    if (entityManager.HasComponent<LocalTransform>(respawnZoneEntity))
                    {
                        //On le recupére
                        LocalTransform respawnZoneTransform = entityManager.GetComponentData<LocalTransform>(respawnZoneEntity);

                        playerTransform.Position = respawnZoneTransform.Position;
                    }
                    else 
                        Debug.LogWarning($"Respawn zone entity {respawnZoneEntity} does not have a LocalTransform component.");
                }
                else 
                    Debug.LogWarning($"No respawn zones found for team entity {player.teamEntity}.");
            }
            else 
                Debug.LogWarning($"Team entity {player.teamEntity} does not have a RespawnZones component.");
        }
    }
}

