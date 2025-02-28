using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public class CharacterGameObjectPrefab : IComponentData
{
    public GameObject GameObjectPrefab;
    public float3 DeltaPosition;
    public string HeadBoneName;
    public string ViewBoneName;
}

public class CharacterAnimatorReference : ICleanupComponentData
{
    public Animator Animator;
    public float3 DeltaPosition;
}

public class CharacterModelBones : IComponentData
{
    public Transform HeadBoneTransform;
    public Transform ViewBoneTransform;
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
    public string HeadBoneName;
    public string ViewBoneName;

    public class Baker : Baker<CharacterAnimatorAuthoring>
    {
        public override void Bake(CharacterAnimatorAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, new CharacterGameObjectPrefab 
            {
                GameObjectPrefab = authoring.CharacterGameObject,
                DeltaPosition = authoring.DeltaPosition.position,
                HeadBoneName = authoring.HeadBoneName,
                ViewBoneName = authoring.ViewBoneName,
            });
            AddComponent(entity, new CharacterAnimationState
            {
                IsWalking = false,
            });
        }
    }
}
