using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

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

    private static float Angle(float3 u, float3 v)
    {
        return math.acos(math.dot(u, v) / (math.length(u) * math.length(v)));
    }

    private static float3 SlopeMovementDirection(float3 moveDir, float3 groundNormal)
    {
        return math.normalize(ProjectOnPlan(moveDir, groundNormal));
    }

    private bool OnSlope(float3 groundNormal)
    {
        float maxAngle = 50; //TODO: Avoid Magic Numbers lul
        float angle = Angle(groundNormal, math.up());
        return angle != 0f && angle <= maxAngle;
    }

    public void Execute(Entity entity, ref CharacterInput input, RefRW<CharacterComponent> characterController,
        RefRW<LocalTransform> localTransform, RefRW<PhysicsVelocity> physicsVelocity)
    {
        ref CharacterComponent controller = ref characterController.ValueRW;
        ref LocalTransform characterTransform = ref localTransform.ValueRW;
        ref PhysicsVelocity vel = ref physicsVelocity.ValueRW;

        float3 feetPosition = characterTransform.Position - new float3(0, 0.95f, 0);

        NativeList<Unity.Physics.ColliderCastHit> allHits = new NativeList<Unity.Physics.ColliderCastHit>(Allocator.Temp);
        //NativeList<Unity.Physics.RaycastHit> allHits = new NativeList<Unity.Physics.RaycastHit>(Allocator.Temp);
        controller.isGrounded = false;
        float3 groundNormal = math.up();

        RaycastInput raycastInput = new RaycastInput()
        {
            Start = feetPosition,
            End = feetPosition + math.down() * 0.15f,
            Filter = CollisionFilter.Default
        };

        /*if (physicsWorld.CastRay(raycastInput, ref allHits))
        {
            foreach (var hit in allHits)
            {
                if (hit.Entity != entity)
                {
                    controller.isGrounded = true;
                    groundNormal = hit.SurfaceNormal;
                    break;
                }
            }
        }*/

        if (physicsWorld.BoxCastAll(feetPosition, characterTransform.Rotation, new float3(.4f, .01f, .4f), math.down(), .15f, ref allHits, CollisionFilter.Default))
        {
            foreach (var hit in allHits)
            {
                if (hit.Entity != entity)
                {
                    controller.isGrounded = true;
                    groundNormal = hit.SurfaceNormal;
                    break;
                }
            }
        }

        bool onSlope = OnSlope(groundNormal) && controller.isGrounded;

        float3 moveDir = math.normalize(math.rotate(characterTransform.Rotation, new float3(input.move.x, 0, input.move.y)));

        bool isMoving = math.lengthsq(moveDir) > 0;

        if (isMoving)
        {
            controller.direction = SlopeMovementDirection(moveDir, groundNormal);
        }
        else
        {
            controller.direction = float3.zero;
        }

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
                controller.currentSpeed = math.min(controller.maxRunningSpeed, controller.currentSpeed + controller.acceleration * decelerationFactor * dt);
            }
        }

        vel.Linear += controller.direction * (controller.acceleration * dt);

        if(!isMoving)
        {
            vel.Linear.x *= (1.0f - controller.linearDampingXZ);
            vel.Linear.z *= (1.0f - controller.linearDampingXZ);
        }

        if(onSlope)
        {
            if(math.length(vel.Linear) > (controller.isWalking ? controller.maxWalkingSpeed : controller.maxRunningSpeed))
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

        //if(!onSlope)
            vel.Linear.y += gravity * dt; //m.s^-2 * s = m.s^-1 (Force)

        if (onSlope && !isMoving) //Prevents jumping when stopping on a slope
            vel.Linear.y = 0;

        //if (controller.isGrounded && vel.Linear.y <= 0.1f) vel.Linear.y = .0f;

        double x = System.Math.Round(vel.Linear.x, 2);
        double y = System.Math.Round(vel.Linear.y, 2);
        double z = System.Math.Round(vel.Linear.z, 2);

        Debug.Log($"Final Velocity: [{x}, {y}, {z}]");

        x = System.Math.Round(groundNormal.x, 2);
        y = System.Math.Round(groundNormal.y, 2);
        z = System.Math.Round(groundNormal.z, 2);

        Debug.Log($"Ground Normal: [{x}, {y}, {z}]");

        // TODO:
        // Easeout la vélocité quand on s'approche de la maxSpeed

        if (input.jump.IsSet && controller.isGrounded)
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
