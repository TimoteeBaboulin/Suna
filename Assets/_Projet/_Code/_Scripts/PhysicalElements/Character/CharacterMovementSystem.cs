using System;
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

    const float gravity = -9.81f;

    private static float3 ProjectOnPlan(float3 vec, float3 normal)
    {
        return vec - math.project(vec, normal);
    }

    public void Execute(Entity entity, ref CharacterInput input, RefRW<CharacterComponent> characterController,
        RefRW<LocalTransform> localTransform, RefRW<PhysicsVelocity> physicsVelocity)
    {
        ref CharacterComponent controller = ref characterController.ValueRW;
        ref LocalTransform characterTransform = ref localTransform.ValueRW;
        ref PhysicsVelocity vel = ref physicsVelocity.ValueRW;

        float3 feetPosition = characterTransform.Position - new float3(0, 0.95f, 0);

        NativeList<Unity.Physics.ColliderCastHit> allHits = new NativeList<Unity.Physics.ColliderCastHit>(Allocator.Temp);
        controller.isGrounded = false;
        float3 slopeDirection = math.forward();
        float3 groundNormal = math.up();

        if (physicsWorld.BoxCastAll(feetPosition, characterTransform.Rotation, new float3(.4f, .01f, .4f), math.down(), .15f, ref allHits, CollisionFilter.Default))
        {
            foreach (var hit in allHits)
            {
                if (hit.Entity != entity)
                {
                    controller.isGrounded = true;
                    slopeDirection = math.cross(math.cross(math.up(), hit.SurfaceNormal), hit.SurfaceNormal);
                    groundNormal = hit.SurfaceNormal;
                    break;
                }
            }
        }

        float x = input.move.x;
        float z = input.move.y;

        bool isMoving = math.length(new float2(x, z)) > 0;

        float3 forward = math.rotate(characterTransform.Rotation, math.forward());
        float3 right = math.cross(math.up(), forward);

        controller.horizontalDir = new float2(x, z);

        if (isMoving)
        {
            controller.direction = math.normalize(ProjectOnPlan(forward, groundNormal) * z + ProjectOnPlan(right, groundNormal) * x);
            float3 dirFromGround = math.normalize(ProjectOnPlan(controller.direction, math.up()));

            if(dirFromGround.y != 0) UnityEngine.Debug.Log("DIR FROM GROUND: " + dirFromGround);

            controller.horizontalDir = new float2(dirFromGround.x, dirFromGround.z);
            UnityEngine.Debug.Log("Direction: " + controller.direction);
        }
        else
        {
            controller.direction = float3.zero;
        }

        controller.horizontalDir = math.normalize(controller.horizontalDir);

        /*float3 dir = math.rotate(characterTransform.Rotation, new float3(controller.direction.x, 0, controller.direction.y));
        controller.direction = new float3(dir.x, 0, dir.z);*/

        float decelerationFactor = math.dot(controller.horizontalDir, controller.inertia) < 0 ? controller.decelerationFactor : 1.0f;

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

        controller.inertia += controller.horizontalDir * (controller.acceleration * dt);

        if (math.length(controller.inertia) > (controller.isWalking ? controller.maxWalkingSpeed : controller.maxRunningSpeed))
        {
            controller.inertia = math.normalize(controller.inertia) * controller.currentSpeed;
        }

        if (math.dot(controller.inertia, controller.horizontalDir) <= 0)
        {
            controller.inertia *= (1.0f - controller.drag);
        }

        vel.Linear.x = controller.inertia.x;
        vel.Linear.z = controller.inertia.y;
        vel.Linear.y += gravity * dt;
        vel.Linear.y += controller.direction.y;

        if (controller.isGrounded && vel.Linear.y <= 0.1f) vel.Linear.y = .0f;

        if (!isMoving) UnityEngine.Debug.Log("Velocity y: " + vel.Linear.y);

        // TODO:
        // Easeout la vélocité quand on s'approche de la maxSpeed

        if (input.jump.IsSet && controller.isGrounded && vel.Linear.y <= 0.1f)
        {
            vel.Linear.y = characterController.ValueRW.jumpForce;
            controller.isGrounded = false;
        }

        if (input.walkStarted.IsSet)
        {
            controller.isWalking = true;
        }

        if (input.walkCanceled.IsSet)
        {
            controller.isWalking = false;
        }
    }
}
