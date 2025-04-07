using Unity.Entities;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour
{
    public TeamSideType side;

    public class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new SpawnerComponent
            {
                side = authoring.side,
            });

            if (authoring.side == TeamSideType.Corpo)
                AddComponent(entity, new CorpoTeamTag { });
            else
                AddComponent(entity, new NatifTeamTag { });
        }
    }
}
