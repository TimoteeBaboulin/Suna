using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
public partial struct CharacterMovementSystem : ISystem
{

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<
            CharacterInput,
            CharacterComponent,
            LocalTransform,
            PhysicsVelocity>(); //Reduce this to only playerInputData to get only the player, all the rest is useful but not needed
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
		
        GameResourcesDatabase database = SystemAPI.GetSingleton<GameResourcesDatabase>();
        NativeHashMap<Entity, RangedWeaponCommonData> weaponData = new(10, Allocator.TempJob); //Do I need more than 10 ? Since there's 10 players playing top

        foreach (var (databaseAccessRO, ownerRef, weapon) in SystemAPI
        .Query<RefRO<RangedWeaponDatabaseAccess>, RefRW<StuffOwner>>()
        .WithAll<IsStuffInHand>()
        .WithEntityAccess())
        {
            ref RangedWeaponCommonData commonData = ref databaseAccessRO.ValueRO.GetData(ref database);
            ref readonly Entity owner = ref ownerRef.ValueRO.Value;

            weaponData[owner] = commonData;
        }

        CharacterMovementJob job = new CharacterMovementJob
        {
            dt = SystemAPI.Time.DeltaTime,
            networkTime = SystemAPI.GetSingleton<NetworkTime>(),
            ecb = ecb.AsParallelWriter(),
            physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
            ccLookup = state.GetComponentLookup<CharacterColliderDataComponent>(),
            StuffListLookup = state.GetComponentLookup<CharacterStuffList>(),
            InHandLookup = state.GetComponentLookup<IsStuffInHand>(),
            CommonDataMap = weaponData,
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        weaponData.Dispose();
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct CharacterMovementJob : IJobEntity
{
    public float dt;
    public NetworkTime networkTime;
    public EntityCommandBuffer.ParallelWriter ecb;
    [ReadOnly] public PhysicsWorld physicsWorld;
    [ReadOnly] public ComponentLookup<CharacterColliderDataComponent> ccLookup;
    [ReadOnly] public ComponentLookup<CharacterStuffList> StuffListLookup;
    [ReadOnly] public ComponentLookup<IsStuffInHand> InHandLookup;
    [ReadOnly] public NativeHashMap<Entity, RangedWeaponCommonData> CommonDataMap;

    private static float3 ProjectOnPlan(float3 vec, float3 normal)
    {
        return vec - math.project(vec, normal);
    }

    private static float Angle(float3 u, float3 v)
    {
        return math.acos(math.dot(u, v) / (math.length(u) * math.length(v)));
    }

    private static float3 SlopeMovementDirection(float3 moveDir, float3 groundNormal)
    {
        return math.normalize(ProjectOnPlan(moveDir, groundNormal));
    }

    private bool OnSlope(float3 groundNormal, float maxSlopeAngle)
    {
        float angle = Angle(groundNormal, math.up());
        return angle != 0f && angle <= maxSlopeAngle;
    }

    public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, ref CharacterInput input, RefRW<CharacterComponent> characterController,
        RefRW<LocalTransform> localTransform, RefRW<PhysicsVelocity> physicsVelocity)
    {
        ref CharacterComponent controller = ref characterController.ValueRW;
        ref LocalTransform characterTransform = ref localTransform.ValueRW;
        ref PhysicsVelocity vel = ref physicsVelocity.ValueRW;

        float3 feetPosition = characterTransform.Position - new float3(0, 0.95f, 0);
        float3 moveDir = math.normalize(math.rotate(characterTransform.Rotation, new float3(input.move.x, 0, input.move.y)));
        bool isMoving = math.lengthsq(moveDir) > 0;
        float3 viewForward = math.normalize(math.rotate(characterTransform.Rotation, math.forward()));

        NativeList<Unity.Physics.ColliderCastHit> allHits = new NativeList<Unity.Physics.ColliderCastHit>(Allocator.Temp);
        NativeList<Unity.Physics.RaycastHit> allHitsFront = new NativeList<Unity.Physics.RaycastHit>(Allocator.Temp);
        controller.isGrounded = false;
        float3 groundNormal = math.up();

        bool forwardHit = false;

        if (isMoving)
        {
            AnimationUtils.AddBoolCommandJob("IsWalking", true, entity, ecb, sortKey);

            float3 forwardHitEnd = feetPosition + (isMoving ? moveDir * 0.45f : viewForward * 0.45f);

            RaycastInput raycastInput = new RaycastInput()
            {
                Start = feetPosition,
                End = forwardHitEnd,
                Filter = CollisionFilter.Default
            };

            if (physicsWorld.CastRay(raycastInput, ref allHitsFront))
            {
                foreach (var hit in allHitsFront)
                {
                    if (hit.Entity != entity)
                    {
                        forwardHit = true;
                        break;
                    }
                }
            }
        }
        else
        {
            AnimationUtils.AddBoolCommandJob("IsWalking", false, entity, ecb, sortKey);
        }

        if (physicsWorld.BoxCastAll(feetPosition, characterTransform.Rotation, new float3(.2f, .01f, .2f), math.down(), .05f, ref allHits, CollisionFilter.Default))
        {
            foreach (var hit in allHits)
            {
                if (hit.Entity == entity)
                    continue;

                if (ccLookup.TryGetComponent(hit.Entity, out CharacterColliderDataComponent ccdc))
                    if (ccdc.CharacterEntity == entity)
                        continue;

                controller.isGrounded = true;
                groundNormal = hit.SurfaceNormal;
                break;
            }
        }

        bool onSlope = OnSlope(groundNormal, controller.maxSlopeAngle) && controller.isGrounded;

        if (controller.isGrounded && controller.isJumping && vel.Linear.y > 0)
        {
            controller.isGrounded = false;
        }

        if (isMoving && Angle(math.up(), groundNormal) < controller.maxSlopeAngle)
        {
            controller.direction = SlopeMovementDirection(moveDir, forwardHit && onSlope ? math.up() : groundNormal);
        }
        else
        {
            controller.direction = float3.zero;
        }

        float weaponSpeedModifier = CommonDataMap.ContainsKey(entity) ? CommonDataMap[entity].coefModifMoveSpeed : 1.0f;

        float decelerationFactor = math.dot(controller.direction, vel.Linear) < 0 ? controller.decelerationFactor : 1.0f;

        if (!isMoving)
        {
            controller.currentSpeed = math.max(0, controller.currentSpeed - controller.deceleration * dt);
        }
        else
        {
            if (controller.isWalking)
            {
                controller.currentSpeed = math.min(controller.maxWalkingSpeed, controller.currentSpeed + controller.acceleration * decelerationFactor * dt);
            }
            else
            {
                controller.currentSpeed = math.min(controller.maxRunningSpeed * weaponSpeedModifier, controller.currentSpeed + controller.acceleration * decelerationFactor * dt);
            }
        }

        vel.Linear += controller.direction * ((controller.isGrounded ? controller.acceleration : controller.acceleration * 0.1f) * dt);

        if (!isMoving)
        {
            vel.Linear.x *= (1.0f - controller.linearDampingXZ);
            vel.Linear.z *= (1.0f - controller.linearDampingXZ);
        }

        if (onSlope && !controller.isJumping)
        {
            if (math.length(vel.Linear) > (controller.isWalking ? controller.maxWalkingSpeed : controller.maxRunningSpeed))
            {
                vel.Linear = math.normalize(vel.Linear) * controller.currentSpeed;
            }
        }
        else
        {
            float2 velXZ = new float2(vel.Linear.x, vel.Linear.z);

            if (math.length(velXZ) > (controller.isWalking ? controller.maxWalkingSpeed : controller.maxRunningSpeed))
            {
                float3 normVel = math.normalize(vel.Linear);
                vel.Linear.x = normVel.x * controller.currentSpeed;
                vel.Linear.z = normVel.z * controller.currentSpeed;
            }
        }

        if (math.dot(vel.Linear, controller.direction) < 0)
        {
            vel.Linear.x *= (1.0f - controller.drag);
            vel.Linear.z *= (1.0f - controller.drag);
        }

        vel.Linear.y += ((controller.isGrounded && !forwardHit) ? 10 : 1) * controller.gravityScale * (-9.81f) * dt; //Applying gravity as force (ms.s^-2 * s = m.s^-1)

        if (onSlope && !isMoving && !controller.isJumping) //Prevents jumping when stopping on a slope
            vel.Linear.y = 0;

        if (controller.isGrounded)
        {
            controller.isJumping = false;
        }

        if (input.jump.IsSet && controller.isGrounded)
        {
            vel.Linear.y = characterController.ValueRW.jumpForce;
            controller.isGrounded = false;
            controller.isJumping = true;
        }

        if (input.walkStarted.IsSet && controller.isGrounded && !controller.isJumping)
        {
            controller.isWalking = true;
        }

        if (input.walkCanceled.IsSet || !controller.isGrounded || controller.isJumping)
        {
            controller.isWalking = false;
        }

        if(input.aimingStarted.IsSet)
        {
            controller.isAiming = true;
        }

        if(input.aimingCanceled.IsSet)
        {
            controller.isAiming = false;
        }
    }
}
