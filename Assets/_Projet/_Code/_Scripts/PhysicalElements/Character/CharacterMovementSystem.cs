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
        CharacterMovementJob job = new CharacterMovementJob
        {
            dt = SystemAPI.Time.DeltaTime,
            networkTime = SystemAPI.GetSingleton<NetworkTime>(),
            physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct CharacterMovementJob : IJobEntity
{
    public float dt;
    public NetworkTime networkTime;
    [ReadOnly] public PhysicsWorld physicsWorld;

    public void Execute(Entity entity, ref CharacterInput input, RefRW<CharacterComponent> characterController,
        RefRW<LocalTransform> localTransform, RefRW<PhysicsVelocity> physicsVelocity)
    {
        ref CharacterComponent controller = ref characterController.ValueRW;
        ref LocalTransform characterTransform = ref localTransform.ValueRW;
        ref PhysicsVelocity vel = ref physicsVelocity.ValueRW;

        float3 feetPosition = characterTransform.Position - new float3(0, 0.95f, 0);
        float3 checkPosition = feetPosition - new float3(0, 0.15f, 0);

        RaycastInput raycastInput = new RaycastInput()
        {
            Start = feetPosition,
            End = checkPosition,
            Filter = CollisionFilter.Default
        };

        NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(Allocator.Temp);
        controller.isGrounded = false;

        if (physicsWorld.CastRay(raycastInput, ref allHits))
        {
            foreach (var hit in allHits)
            {
                if (hit.Entity != entity)
                {
                    controller.isGrounded = true;
                    break;
                }
            }
        }

        float x = input.move.x;
        float z = input.move.y;

        if (math.length(new float2(x, z)) > 0)
        {
            controller.direction = math.normalize(new float2(x, z));
        }
        else
        {
            controller.direction = float2.zero;
        }

        float3 dir = math.rotate(characterTransform.Rotation, new float3(controller.direction.x, 0, controller.direction.y));
        controller.direction = new float2(dir.x, dir.z);

        float decelerationFactor = math.dot(controller.direction, controller.inertia) < 0 ? controller.decelerationFactor : 1.0f;

        if (math.length(controller.direction) < math.EPSILON)
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
                controller.currentSpeed = math.min(controller.maxRunningSpeed, controller.currentSpeed + controller.acceleration * decelerationFactor * dt);
            }
        }

        controller.inertia += controller.direction * (controller.acceleration * dt);
        if (math.length(controller.inertia) > (controller.isWalking ? controller.maxWalkingSpeed : controller.maxRunningSpeed))
        {
            controller.inertia = math.normalize(controller.inertia) * controller.currentSpeed;
        }

        if (math.dot(controller.inertia, controller.direction) <= 0)
        {
            controller.inertia *= (1.0f - controller.drag);
        }

        vel.Linear = new float3(controller.inertia.x, vel.Linear.y, controller.inertia.y);

        // TODO:
        // Easeout la vélocité quand on s'approche de la maxSpeed
        // Fix le problème de friction avec les autres collider (lors du saut en appuyant sur Z)

        if (input.jump.IsSet && controller.isGrounded)
        {
            physicsVelocity.ValueRW.Linear.y = characterController.ValueRW.jumpForce;
            characterController.ValueRW.isGrounded = false;
        }

        if (input.walkStarted.IsSet)
        {
            characterController.ValueRW.isWalking = true;
        }

        if (input.walkCanceled.IsSet)
        {
            characterController.ValueRW.isWalking = false;
        }
    }
}
