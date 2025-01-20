using Unity.Entities;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CharacterControllerAuthoring : MonoBehaviour
{
    [Header("Movement Parameters")]
    public float maxRunningSpeed = 1.5f;
    public float maxWalkingSpeed = 0.5f;
    public float maxStepHeight = 0.5f;

    [Header("Vertical Movement Parameters")]
    public float gravity = 9.81f;
    public float jumpForce = 3f;
    public float drag = 0.1f;

    [Header("Camera Parameters")]
    public float sensivity = 1f;

    public bool lockRotationX = false;
    public bool lockRotationY = false;
    public bool lockRotationZ = false;

    public class Baker : Baker<CharacterControllerAuthoring>
    {
        public override void Bake(CharacterControllerAuthoring cca)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new CharacterControllerComponent
            {
                currentSpeed = 0f,
                maxRunningSpeed = cca.maxRunningSpeed,
                maxWalkingSpeed = cca.maxWalkingSpeed,
                maxStepHeight = cca.maxStepHeight,
                gravity = cca.gravity,
                jumpForce = cca.jumpForce,
                drag = cca.drag,
                jumpRequest = false,
                isGrounded = false,
                sensivity = cca.sensivity
            });

            AddComponent(entity, new CameraAttachComponent());

            AddComponent(entity, new FreezeAllRotationTag());

            /*AddComponent(entity, new LocalTransform());
            AddComponent(entity, new PhysicsVelocity());*/
        }
    }
}
