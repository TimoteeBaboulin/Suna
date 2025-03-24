using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class HarvesterAuthoring : MonoBehaviour
{
    public class HarvesterBaker : Baker<HarvesterAuthoring>
    {
        public override void Bake(HarvesterAuthoring authoring)
        {
            HarvesterComponent harvester = new HarvesterComponent
            {
                Owner = Entity.Null
            };
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, harvester);
        }
    }
}
