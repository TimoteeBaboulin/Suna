using Unity.Entities;
using UnityEngine;

class CharacterColliderAuthoring : MonoBehaviour
{

}

class CharacterColliderAuthoringBaker : Baker<CharacterColliderAuthoring>
{
    public override void Bake(CharacterColliderAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent<CharacterColliderTag>(entity);
        AddComponent<CharacterColliderInitEntityTag>(entity);
        AddComponent<CharacterColliderComponent>(entity);
    }
}
