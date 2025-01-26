using Unity.Entities;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

public struct CharacterTag : IComponentData
{

}

public sealed class CharacterControllerAuthoring : MonoBehaviour
{
    [Header("Movement Parameters")]
    public float maxRunningSpeed = 1.5f;
    public float maxWalkingSpeed = 0.5f;
    public float acceleration = 3.0f;
    public float deceleration = 4.0f;
    public float decelerationFactor = 1.4f;
    public float drag = 0.1f;
    public float maxStepHeight = 0.5f;

    [Header("Vertical Movement Parameters")]
    public float jumpForce = 3f;

    [Header("Camera Parameters")]
    public float sensivity = 1f;

    public class Baker : Baker<CharacterControllerAuthoring>
    {
        public override void Bake(CharacterControllerAuthoring cca)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new CharacterControllerComponent
            {
                currentSpeed = cca.maxRunningSpeed,
                maxRunningSpeed = cca.maxRunningSpeed,
                maxWalkingSpeed = cca.maxWalkingSpeed,
                acceleration = cca.acceleration,
                deceleration = cca.deceleration,
                decelerationFactor = cca.decelerationFactor,
                drag = cca.drag,
                maxStepHeight = cca.maxStepHeight,
                jumpForce = cca.jumpForce,
                jumpRequest = false,
                isGrounded = false,
                isWalking = false,
                sensivity = cca.sensivity
            });

            AddComponent(entity, new CameraAttachComponent());

            AddComponent(entity, new FreezeAllRotationTag());

            AddComponent(entity, new CharacterTag()); //Multiplayer
            AddComponent(entity, new PlayerInputData()); //Inputs for multiplayer

           // AddComponent<PlayerInputData>(entity); //Inputs for multiplayer
        }
    }
}
