
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public struct CharacterTag : IComponentData { }

[GhostEnabledBit]
public struct CharacterEnableTag : IComponentData, IEnableableComponent { }

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
    [GhostField] public bool isJumping;

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

public struct CharacterStuffPrefab : IComponentData
{
    public Entity MainWeaponPrefab;
    public Entity SecondWeaponPrefab;
    public Entity MeleeWeaponPrefab;
}

[GhostComponent]
public struct CharacterStuffList : IComponentData
{
    [GhostField] public FixedList128Bytes<Entity> Value;
}

[GhostComponent]

public struct CharacterStuffInHandType : IComponentData 
{
    [GhostField] public StuffType Value;
}


