using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[GhostComponent]
public struct CharacterControllerComponent : IComponentData
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
}

public struct CameraAttachComponent : IComponentData
{
    [GhostField] public LocalTransform transform;
    [GhostField] public float cameraPitch;
    [GhostField] public float cameraYaw;
}