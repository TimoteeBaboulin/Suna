using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour
{
    [SerializeField] TeamSideType side;
    [SerializeField] float playerLifeSpaceSize;

    public class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new SpawnerComponent
            {
                side = authoring.side,
                playerLifeSpaceSize = authoring.playerLifeSpaceSize,
            });

            if (authoring.side == TeamSideType.Corpo)
                AddComponent(entity, new CorpoTeamTag { });
            else
                AddComponent(entity, new NatifTeamTag { });
        }
    }
}
