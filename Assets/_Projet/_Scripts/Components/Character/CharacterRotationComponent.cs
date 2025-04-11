using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;

[GhostComponent]
public struct CharacterViewRotation : IComponentData
{
    [GhostField] public float Pitch;
    [GhostField] public quaternion ViewRotation;
    [GhostField] public quaternion ShootingModifier;
}
