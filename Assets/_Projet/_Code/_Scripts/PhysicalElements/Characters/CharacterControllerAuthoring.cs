using Unity.Entities;
using UnityEngine;

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

    [Header("View GameObject")]
    [SerializeField] private GameObject _viewGameObject;

    [Header("Temp(Debug)")]
    public TeamSideType side;

    public class Baker : Baker<CharacterControllerAuthoring>
    {
        public override void Bake(CharacterControllerAuthoring cca)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            if (cca.side == TeamSideType.Corpo)
                AddComponent(entity, new CorpoTeamTag { });
            else
                AddComponent(entity, new NatifTeamTag { });

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
                sensivity = cca.sensivity,
            });

            AddComponent(entity, new CharacterViewComponent
            {
                View = GetEntity(cca._viewGameObject, TransformUsageFlags.Dynamic),
            });

            AddComponent(entity, new CameraAttachComponent());

            AddComponent(entity, new FreezeAllRotationTag());

            AddComponent(entity, new CharacterTag()); //Multiplayer
            AddComponent(entity, new CharacterInput()); //Inputs for multiplayer
            AddComponent(entity, new HasHitComponent { Value = false });
            AddComponent(entity, new WaitForRespawnTag { });

            // AddComponent<PlayerInputData>(entity); //Inputs for multiplayer
        }
    }
}
