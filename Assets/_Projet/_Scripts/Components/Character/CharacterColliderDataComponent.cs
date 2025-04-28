using Unity.Entities;

public struct CharacterColliderDataComponent : IComponentData
{
    public Entity CharacterEntity;
    public float DamageMultiplier;
    public CharacterColliderType Type;
}
