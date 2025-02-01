using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

public struct CharacterTag : IComponentData { }

[GhostComponent]
public struct CharacterComponent : IComponentData
{
    [GhostField] public float currentSpeed;
    [GhostField] public float maxRunningSpeed;
    [GhostField] public float maxWalkingSpeed;

    [GhostField] public float deceleration;
    [GhostField] public float acceleration;
    [GhostField] public float decelerationFactor;
    [GhostField] public float drag;

    [GhostField] public float2 direction;
    [GhostField] public float2 inertia;

    [GhostField] public float maxStepHeight;

    [GhostField] public float jumpForce;

    [GhostField] public bool jumpRequest;
    [GhostField] public bool isGrounded;
    [GhostField] public bool isWalking;

    [GhostField] public float sensivity;

    [GhostField] public float verticalCameraAngle;

    [GhostField] public Entity teamEntity;
}