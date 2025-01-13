using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

public struct CharacterControllerComponent : IComponentData
{
    public float currentSpeed;
    public float maxRunningSpeed;
    public float maxWalkingSpeed;
    public float maxStepHeight;

    public float gravity;
    public float jumpForce;
    public float drag;

    public bool jumpRequest;
    public bool isGrounded;

    public float sensivity;

    public bool lockRotationX;
    public bool lockRotationY;
    public bool lockRotationZ;

    public float verticalCameraAngle;
}

public struct CameraAttachComponent : IComponentData
{
    public LocalTransform transform;
}