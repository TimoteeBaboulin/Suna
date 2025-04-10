using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct CharacterTag : IComponentData { }

[GhostEnabledBit]
public struct CharacterIsEnable : IComponentData, IEnableableComponent { }

public struct CharacterDeadColliderTag : IComponentData { }

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

    [GhostField] public float gravityScale;

    [GhostField] public float3 direction;
    [GhostField] public float2 horizontalDir;
    [GhostField] public float2 inertia;

    [GhostField] public float maxSlopeAngle;

    [GhostField] public float linearDampingXZ;

    [GhostField] public float maxStepHeight;

    [GhostField] public float jumpForce;
    [GhostField] public bool isJumping;

    [GhostField] public bool jumpRequest;
    [GhostField] public bool isGrounded;
    [GhostField] public bool isWalking;
    [GhostField] public bool isOnSite;
    [GhostField] public bool isAiming;

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

public struct CharacterShootStartPositionDelta : IComponentData
{
    public float3 PositionDelta;
}

// This value is used for calculations that require the character's view rotation.
public struct CharacterLocalViewRotation : IComponentData
{
    public quaternion ViewRotation;
    [GhostField] public quaternion ShootingModifier;
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

public struct CharacterDefaultStuffName : IBufferElementData
{
    public FixedString128Bytes Value;
}

[GhostComponent]
public struct CharacterStuffList : IComponentData
{
    [GhostField] public FixedList128Bytes<Entity> Value;
}

[GhostComponent]

public struct CharacterStuffInHandLocation : IComponentData 
{
    [GhostField] public StuffInventoryLocation Value;
}

[GhostEnabledBit]
public struct IsInstanciateDefaultStuff : IComponentData, IEnableableComponent { }

[GhostComponent]
public struct CharacterMoney : IComponentData
{
    [GhostField] public uint money;
    [GhostField] public uint maxMoney;
}