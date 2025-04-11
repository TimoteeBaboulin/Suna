using Unity.Entities;
using UnityEngine;

class AnimationAuthoring : MonoBehaviour
{
    
}

class AnimationAuthoringBaker : Baker<AnimationAuthoring>
{
    public override void Bake(AnimationAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponentObject(entity, new AnimatorReference
        {
            Animator = null,
        });

        AddComponent<AnimationFloatBufferElement>(entity);
        AddComponent<AnimationIntBufferElement>(entity);
        AddComponent<AnimationBoolBufferElement>(entity);
        AddComponent<AnimationTriggerBufferElement>(entity);
    }
}
