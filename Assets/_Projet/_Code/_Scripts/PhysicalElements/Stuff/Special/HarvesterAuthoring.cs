using Unity.Entities;
using UnityEngine;

public class HarvesterAuthoring : MonoBehaviour
{
    public class HarvesterBaker : Baker<HarvesterAuthoring>
    {
        public override void Bake(HarvesterAuthoring authoring)
        {
            HarvesterComponent harvester = new HarvesterComponent
            {
                ownerNetworkId = -1
            };
            Entity entity = GetEntity(TransformUsageFlags.WorldSpace);
            AddComponent(entity, harvester);
        }
    }
}
