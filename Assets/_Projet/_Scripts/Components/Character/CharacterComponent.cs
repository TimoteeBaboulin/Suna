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
    [GhostField] public bool isShooting;

    [GhostField] public float verticalCameraAngle;

    [GhostField] public Entity teamEntity;
}

public struct CharacterShootStartPositionDelta : IComponentData
{
    public float3 PositionDelta;
}

public struct CharacterClientAttachedComponent : IComponentData
{
    [GhostField] public Entity ClientEntity;
}

[GhostComponent]
public struct CharacterDefaultStuffName : IBufferElementData
{
    [GhostField] public FixedString128Bytes Value;
}

//[GhostComponent]
//public struct CharacterStuffList : IComponentData
//{
//    [GhostField] public FixedList128Bytes<Entity> List;
//    [GhostField] public StuffSlot StuffInHandSlot;

//    public Entity StuffInHand { get => List[(int)StuffInHandSlot]; set => List[(int)StuffInHandSlot] = value; }

//    public Entity GetStuffInSlot(StuffSlot slot)
//    {
//        return List[(int)slot];
//    }

//    public void SetStuffInSlot(StuffSlot slot, Entity stuff)
//    {
//        List[(int)slot] = stuff;
//    }
//}

[GhostComponent]
public struct CharacterStuffInfos : IComponentData
{
    [GhostField] public StuffSlot StuffInHandSlot;
}

[GhostComponent]
[InternalBufferCapacity((int)StuffSlot.nbSlots)]
public struct CharacterStuffList: IBufferElementData
{
    [GhostField] public Entity entity;
}


[GhostEnabledBit]
public struct IsInstanciateDefaultStuff : IComponentData, IEnableableComponent { }

[GhostComponent]
public struct CharacterMoney : IComponentData
{
    [GhostField] public uint money;
    [GhostField] public uint maxMoney;
}

public struct SmoothInput : IComponentData
{
    public float2 Current;
}

[GhostEnabledBit]
public struct CharacterIsDifusing : IComponentData, IEnableableComponent { }