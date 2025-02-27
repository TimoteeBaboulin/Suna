using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public class CharacterGameObjectPrefab : IComponentData
{
    public GameObject GameObjectPrefab;
    public float3 DeltaPosition;
}

public class CharacterAnimatorReference : ICleanupComponentData
{
    public Animator Animator;
    public float3 DeltaPosition;
}

[GhostComponent]
public struct CharacterAnimationState : IComponentData
{
    [GhostField] public bool IsWalking;
}

public class CharacterAnimatorAuthoring : MonoBehaviour
{
    public GameObject CharacterGameObject;
    public Transform DeltaPosition;

    public class Baker : Baker<CharacterAnimatorAuthoring>
    {
        public override void Bake(CharacterAnimatorAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, new CharacterGameObjectPrefab 
            {
                GameObjectPrefab = authoring.CharacterGameObject,
                DeltaPosition = authoring.DeltaPosition.position
            });
            AddComponent(entity, new CharacterAnimationState
            {
                IsWalking = false,
            });
        }
    }
}
