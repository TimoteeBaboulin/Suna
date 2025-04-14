using Unity.Entities;
using UnityEngine;

class TracerRoundAuthoring : MonoBehaviour
{
    public float Speed = 15;
}

class TracerRoundAuthoringBaker : Baker<TracerRoundAuthoring>
{
    public override void Bake(TracerRoundAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        TracerRoundComponent component = new TracerRoundComponent
        {
            speed = authoring.Speed
        };
        AddComponent(entity, component);
    }
}
