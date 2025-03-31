using Unity.Entities;
using UnityEngine;

class ThirdPersonCharacterAnimationAuthoring : MonoBehaviour
{
    
}

class ThirdPersonCharacterAnimationAuthoringBaker : Baker<ThirdPersonCharacterAnimationAuthoring>
{
    public override void Bake(ThirdPersonCharacterAnimationAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ThirdPersonCharacterAnimationState
        {
            IsWalking = false,
        });
    }
}
