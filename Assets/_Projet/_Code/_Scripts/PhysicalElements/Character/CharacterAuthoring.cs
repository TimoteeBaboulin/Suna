using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.PackageManager;
using UnityEngine;

public sealed class CharacterAuthoring : MonoBehaviour
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

    [Header("Temp(Debug)")]
    public TeamSideType side;
    public GameObject mainWeaponPrefab;
    public GameObject secondWeaponPrefab;
    public GameObject meleeWeaponPrefab;

    [Header("Visual")]
    [SerializeField] private GameObject _view;

    public class Baker : Baker<CharacterAuthoring>
    {
        public override void Bake(CharacterAuthoring cca)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            if (cca.side == TeamSideType.Corpo)
                AddComponent(entity, new CorpoTeamTag { });
            else
                AddComponent(entity, new NatifTeamTag { });

            AddComponent(entity, new CharacterComponent
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
            });

            AddComponent(entity, new CharacterStuffPrefab
            {
                MainWeaponPrefab = GetEntity(cca.mainWeaponPrefab, TransformUsageFlags.Dynamic),
                SecondWeaponPrefab = GetEntity(cca.secondWeaponPrefab, TransformUsageFlags.Dynamic),
                MeleeWeaponPrefab = GetEntity(cca.meleeWeaponPrefab, TransformUsageFlags.Dynamic)
            });

            AddComponent(entity, new FreezeAllRotationTag());

            AddComponent(entity, new CharacterTag()); //Multiplayer
            AddComponent(entity, new CharacterInput()); //Inputs for multiplayer
            AddComponent(entity, new HasHitComponent { Value = false });
            AddComponent(entity, new WaitForRespawnTag { });
            AddComponent(entity, new WaitForInstanciateWeaponsTag { });

            AddComponent(entity, new CharacterClientAttachedComponent { ClientEntity = Entity.Null });

            AddComponent(entity, new CharacterAndViewRotationComponent
            {
                CharacterRotation = quaternion.identity,
                ViewRotation = quaternion.identity,
            });
            AddComponent(entity, new CharacterLocalViewRotation { ViewRotation = quaternion.identity });

            CharacterStuffList weapons = new CharacterStuffList();
            for (int i = 0; i < 3; i++) weapons.List.Add(Entity.Null);
            AddComponent(entity, weapons);

            AddComponent(entity, new CharacterStuffInHandType());

        }
    }
}
