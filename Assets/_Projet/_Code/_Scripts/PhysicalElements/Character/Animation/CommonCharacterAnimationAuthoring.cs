using Unity.Entities;
using UnityEngine;

class CommonCharacterAnimationAuthoring : MonoBehaviour
{
    
}

class CommonCharacterAnimationAuthoringBaker : Baker<CommonCharacterAnimationAuthoring>
{
    public override void Bake(CommonCharacterAnimationAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new CommonCharacterAnimationState
        {
            IsWalking = false,
        });
    }
}
