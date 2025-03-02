using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public class WeaponViewPrefab : IComponentData
{
    public GameObject GameObjectPrefab;
}

public class WeaponAnimatorReference : ICleanupComponentData
{
    public Animator Animator;
    public Transform Transform;
}

[GhostComponent]
public struct WeaponAnimationState : IComponentData
{
    [GhostField] public bool IsFire;
    [GhostField] public bool IsReload;
}

[GhostComponent]
public struct WeaponOwner : IComponentData
{
    [GhostField] public Entity Value;
}

public class WeaponAnimatorAuthoring : MonoBehaviour
{
    public GameObject WeaponGameObject;

    public class Baker : Baker<WeaponAnimatorAuthoring>
    {
        public override void Bake(WeaponAnimatorAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponentObject(entity, new WeaponViewPrefab
            {
                GameObjectPrefab = authoring.WeaponGameObject,
            });

            AddComponent(entity, new WeaponAnimationState
            {
                IsFire = true,
                IsReload = false
            });

            AddComponent<WeaponOwner>(entity);
        }
    }
}
