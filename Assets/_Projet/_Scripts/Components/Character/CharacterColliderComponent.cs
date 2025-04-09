using Unity.Entities;

public struct CharacterColliderTag : IComponentData { }
public struct CharacterColliderInitEntityTag : IComponentData { }

public struct CharacterColliderComponent : IComponentData
{
    public Entity HeadEntity;
    public Entity ArmLeftEntity0;
    public Entity ArmLeftEntity1;
    public Entity ArmLeftEntity2;
    public Entity ArmRightEntity0;
    public Entity ArmRightEntity1;
    public Entity ArmRightEntity2;
    public Entity ThoraxEntity;
    public Entity StomachEntity0;
    public Entity StomachEntity1;
    public Entity LegLeftEntity0;
    public Entity LegLeftEntity1;
    public Entity LegLeftEntity2;
    public Entity LegRightEntity0;
    public Entity LegRightEntity1;
    public Entity LegRightEntity2;
}

