using Unity.Entities;
using Unity.Mathematics;
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
    public float linearDampingXZ = 0.5f;
    public float gravityScale = -1.0f;
    public float maxSlopeAngle = 50.0f;

    [Header("Vertical Movement Parameters")]
    public float jumpForce = 3f;

    [Header("Camera Parameters")]
    public float sensivity = 1f;

    [Header("Temp(Debug)")]
    public TeamSideType side;
    public RangedWeaponData mainWeapon;
    public RangedWeaponData secondWeapon;
    public MeleeWeaponData meleeWeapon;
    public HarvesterData harvester;

    [Header("Shoot Start Position")]
    public Transform shootStartpos;

    public class Baker : Baker<CharacterAuthoring>
    {
        public override void Bake(CharacterAuthoring cca)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            //if (cca.side == TeamSideType.Corpo)
            //    AddComponent(entity, new CorpoTeamTag { });
            //else
            //    AddComponent(entity, new NatifTeamTag { });

            AddComponent(entity, new CharacterComponent
            {
                currentSpeed = cca.maxRunningSpeed,
                maxRunningSpeed = cca.maxRunningSpeed,
                maxWalkingSpeed = cca.maxWalkingSpeed,
                acceleration = cca.acceleration,
                deceleration = cca.deceleration,
                decelerationFactor = cca.decelerationFactor,
                linearDampingXZ = cca.linearDampingXZ,
                drag = cca.drag,
                gravityScale = cca.gravityScale,
                maxStepHeight = cca.maxStepHeight,
                maxSlopeAngle = cca.maxSlopeAngle,
                jumpForce = cca.jumpForce,
                jumpRequest = false,
                isGrounded = false,
                isWalking = false,
            });

            DynamicBuffer<CharacterDefaultStuffName> weaponsBuffer = AddBuffer<CharacterDefaultStuffName>(entity);
            weaponsBuffer.Add(new CharacterDefaultStuffName { Value = cca.mainWeapon != null ? cca.mainWeapon.entityName : "" });
            weaponsBuffer.Add(new CharacterDefaultStuffName { Value = cca.secondWeapon != null ? cca.secondWeapon.entityName : "" });
            weaponsBuffer.Add(new CharacterDefaultStuffName { Value = cca.meleeWeapon != null ? cca.meleeWeapon.entityName : "" });
            weaponsBuffer.Add(new CharacterDefaultStuffName { Value = cca.harvester != null ? cca.harvester.entityName : "" });


            AddComponent(entity, new FreezeAllRotationTag());

            AddComponent<CharacterTag>(entity); //Multiplayer

            AddComponent<CharacterIsEnable>(entity);

            AddComponent(entity, new CharacterInput()); //Inputs for multiplayer

            AddComponent(entity, new HasHitComponent { Value = false });
            AddComponent(entity, new WaitForRespawnTag { });

            AddComponent<IsInstanciateDefaultStuff>(entity);
            SetComponentEnabled<IsInstanciateDefaultStuff>(entity, true);

            AddComponent(entity, new CharacterClientAttachedComponent { ClientEntity = Entity.Null });

            AddComponent(entity, new CharacterAndViewRotationComponent
            {
                CharacterRotation = quaternion.identity,
                ViewRotation = quaternion.identity,
            });
            AddComponent(entity, new CharacterLocalViewRotation { ViewRotation = quaternion.identity });

            CharacterStuffList stuff = new CharacterStuffList();
            for (int i = 0; i < (int)StuffSlot.nbLocation; i++) stuff.List.Add(Entity.Null);
            AddComponent(entity, stuff);

            AddComponent(entity, new CharacterShootStartPositionDelta { PositionDelta = cca.shootStartpos.position });

            AddComponent(entity, new PlayerHarvesterActions
            {
                IsDefusing = false,
                IsPlanting = false
            });
        }
    }
}