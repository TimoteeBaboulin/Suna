using Unity.Entities;
using UnityEngine;

public class TeamSpawnAuthoring : MonoBehaviour
{
    public Transform[] spawnPoints;
    public TeamSideType team;

    public class TeamSpawnBaker : Baker<TeamSpawnAuthoring>
    {
        public override void Bake(TeamSpawnAuthoring authoring)
        {
            if (authoring.spawnPoints is null || authoring.spawnPoints.Length <= 0)
                return;

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new TeamSpawnComponent
            {
                team = authoring.team,
            });

            var Buffer = AddBuffer<SpawnPointBufferComponent>(entity);
            for(int i = 0; i < authoring.spawnPoints.Length; i++)
            {
                Transform point = authoring.spawnPoints[i];
                Vector3 position;
                if (point is null)
                {
                    position = Vector3.zero;
                }
                else
                {
                    position = point.position;
                }

                Buffer.Add(new SpawnPointBufferComponent { Value = position });
            }
        }
    }
}
