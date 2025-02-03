using Unity.Entities;
using UnityEngine;

public class TeamAuthoring : MonoBehaviour
{
    public TeamSideType side;

    public class Baker : Baker<TeamAuthoring>
    {
        public override void Bake(TeamAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            //var buffer = AddBuffer<RespawnPoints>(entity);

            SpawnerAuthoring[] spawns = FindObjectsByType<SpawnerAuthoring>(FindObjectsSortMode.None);

            foreach (var spawnPoint in spawns)
            {
                if (spawnPoint.side == authoring.side)
                {
                    Entity spawnEntity = GetEntity(spawnPoint.gameObject, TransformUsageFlags.Dynamic);

                    //buffer.Add(new RespawnPoints
                    //{
                    //    entity = spawnEntity
                    //});
                }
            }

            if (authoring.side == TeamSideType.Corpo)
                AddComponent(entity, new CorpoTeamTag { });
            else
                AddComponent(entity, new NatifTeamTag { });
        }
    }
}
