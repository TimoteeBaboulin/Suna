using Unity.Entities;
using UnityEngine;

public class ShootAuthoring : MonoBehaviour
{
    public class Baker : Baker<ShootAuthoring>
    {
        public override void Bake(ShootAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ShootInputComponent>(entity);
        }
    }
}
