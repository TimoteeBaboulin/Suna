using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

public struct CharacterControllerComponent : IComponentData
{
    public float currentSpeed;
    public float maxRunningSpeed;
    public float maxWalkingSpeed;

    public float deceleration;
    public float acceleration;
    public float decelerationFactor;
    public float drag;

    public float2 direction;
    public float2 inertia;

    public float maxStepHeight;


    public float jumpForce;

    public bool jumpRequest;
    public bool isGrounded;
    public bool isWalking;

    public float sensivity;

    public float verticalCameraAngle;
}

public struct CameraAttachComponent : IComponentData
{
    public LocalTransform transform;
    public float cameraPitch;
    public float cameraYaw;
}