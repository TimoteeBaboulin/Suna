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

    [GhostField] public float2 direction;
    [GhostField] public float2 inertia;

    [GhostField] public float maxStepHeight;

    [GhostField] public float jumpForce;

    [GhostField] public bool jumpRequest;
    [GhostField] public bool isGrounded;
    [GhostField] public bool isWalking;

    [GhostField] public float verticalCameraAngle;

    [GhostField] public Entity teamEntity;
}

// Allows the server to synchronize the character's rotation and its view with all the clients.
[GhostComponent]
public struct CharacterAndViewRotationComponent : IComponentData
{
    [GhostField] public quaternion CharacterRotation;
    [GhostField] public quaternion ViewRotation;
}

// Store the local rotation value of the character's view.
// This value is used for local calculations that require the character's view rotation.
// This also helps avoid rollbacks and the stuttering that would occur with a value synchronized with the server.
public struct CharacterLocalViewRotation : IComponentData
{
    public quaternion ViewRotation;
}

// RPC message that allows the client to send the rotation values of its character and its view,
// so that the server can update them on its side and synchronize these values with all other clients.
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
