using Unity.Entities;
using UnityEngine;

public class HealthAuthoring : MonoBehaviour
{
    [SerializeField] private int _maxHealth;

    public class Baker : Baker<HealthAuthoring>
    {
        public override void Bake(HealthAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new MaxHealthComponent { Value = authoring._maxHealth });
            AddComponent(entity, new CurrentHealthComponent { Value = authoring._maxHealth});
            AddComponent<DamageBufferElement>(entity);
            AddComponent<DamageThisTickCommand>(entity);
        }
    }
}
