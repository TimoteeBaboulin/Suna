using Unity.Entities;
using UnityEngine;

class SpawnBarrierAuthoring : MonoBehaviour
{
    
}

class SpawnBarrierAuthoringBaker : Baker<SpawnBarrierAuthoring>
{
    public override void Bake(SpawnBarrierAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<SpawnBarrierComponent>(entity);
    }
}
