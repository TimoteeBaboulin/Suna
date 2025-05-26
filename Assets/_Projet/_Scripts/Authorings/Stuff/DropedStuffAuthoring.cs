using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class DropedStuffAuthoring : MonoBehaviour
{
    public class Baker : Baker<DropedStuffAuthoring>
    {
        public override void Bake(DropedStuffAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new StuffEntityInHandRef());
        }
    }
}


