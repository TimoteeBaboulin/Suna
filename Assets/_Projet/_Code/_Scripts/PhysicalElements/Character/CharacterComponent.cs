using Unity.Cinemachine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

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

    [GhostField] public float3 direction;
    [GhostField] public float2 horizontalDir;
    [GhostField] public float2 inertia;

    [GhostField] public float linearDampingXZ;

    [GhostField] public float maxStepHeight;

    [GhostField] public float jumpForce;

    [GhostField] public bool jumpRequest;
    [GhostField] public bool isGrounded;
    [GhostField] public bool isWalking;

    [GhostField] public float verticalCameraAngle;

    [GhostField] public Entity teamEntity;
}

// Do not use the values of this component for calculations.
// It is only meant to synchronize these values between the server and clients.
[GhostComponent]
public struct CharacterAndViewRotationComponent : IComponentData
{
    [GhostField] public quaternion CharacterRotation;
    [GhostField] public quaternion ViewRotation;
}

// This value is used for calculations that require the character's view rotation.
public struct CharacterLocalViewRotation : IComponentData
{
    public quaternion ViewRotation;
}

// To be used for updating the character's rotation and its view from the client to the server.
public struct ClientCharacterAndViewRotationRpcCommand : IRpcCommand
{
    public quaternion ViewRotation;
    public quaternion CharacterRotation;
}

public struct CharacterClientAttachedComponent : IComponentData
{
    [GhostField] public Entity ClientEntity;
}

public struct CharacterDefaultWeaponPrefab : IComponentData
{
    public Entity Value;
}

public struct CharacterDefaultWeapon : IComponentData
{
    [GhostField] public Entity Value;
}
