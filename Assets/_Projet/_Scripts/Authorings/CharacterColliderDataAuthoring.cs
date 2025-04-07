using Unity.Entities;
using UnityEngine;

class CharacterColliderDataAuthoring : MonoBehaviour
{
    public CharacterColliderType ColliderType;
    public CharacterColliderData ColliderData;
}

class CharacterColliderDataAuthoringBaker : Baker<CharacterColliderDataAuthoring>
{
    public override void Bake(CharacterColliderDataAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new CharacterColliderDataComponent
        {
            CharacterEntity = Entity.Null,
            DamageMultiplier = authoring.ColliderType switch
            {
                CharacterColliderType.Head => authoring.ColliderData.HeadMultiplier,
                CharacterColliderType.Arm => authoring.ColliderData.ArmMultiplier,
                CharacterColliderType.Thorax => authoring.ColliderData.ThoraxMultiplier,
                CharacterColliderType.Stomach => authoring.ColliderData.StomachMultiplier,
                CharacterColliderType.Leg => authoring.ColliderData.LegMultiplier,
                _ => 1f,
            }
        });
    }
}
