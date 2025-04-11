using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class CharacterRotationAuthoring : MonoBehaviour
{
    
}

class CharacterRotationAuthoringBaker : Baker<CharacterRotationAuthoring>
{
    public override void Bake(CharacterRotationAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new CharacterViewRotation
        {
            ViewRotation = quaternion.identity,
            ShootingModifier = quaternion.identity,
        });
    }
}
