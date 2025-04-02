using Unity.Entities;
using UnityEngine;

public class BombSiteTriggerAuthoring : MonoBehaviour
{
    public class BombSiteTriggerBaker : Baker<BombSiteTriggerAuthoring>
    {
        public override void Bake(BombSiteTriggerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<BombSiteTriggerComponent>(entity);
        }
    }
}
