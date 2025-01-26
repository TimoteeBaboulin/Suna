using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
//[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial struct PlayerMovementSystem : ISystem
{

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<
            PlayerInput,
            CharacterControllerComponent,
            LocalTransform,
            PhysicsVelocity,
            CameraAttachComponent>(); //Reduce this to only playerInputData to get only the player, all the rest is useful but not needed
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        PlayerMovementJob job = new PlayerMovementJob
        {
            dt = SystemAPI.Time.DeltaTime,
            networkTime = SystemAPI.GetSingleton<NetworkTime>(),
           // worldName = state.World.Name
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct PlayerMovementJob : IJobEntity
{
    public float dt;
    public NetworkTime networkTime;
   // public FixedString32Bytes worldName;

    public void Execute(ref PlayerInput input, RefRW<CharacterControllerComponent> characterController,
        RefRW<LocalTransform> localTransform, RefRW<CameraAttachComponent> cameraAttach, RefRW<PhysicsVelocity> physicsVelocity)
    {
        if (!(networkTime.IsFirstPredictionTick))
        {
            return;
        }

        ref CharacterControllerComponent controller = ref characterController.ValueRW;
        ref LocalTransform playerTransform = ref localTransform.ValueRW;
        ref PhysicsVelocity vel = ref physicsVelocity.ValueRW;
        ref CameraAttachComponent camera = ref cameraAttach.ValueRW;

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

        float3 dir = math.rotate(playerTransform.Rotation, new float3(controller.direction.x, 0, controller.direction.y));
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

        camera.transform.Position = playerTransform.Position;
        camera.transform.Position += new float3(0f, 0.8f, 0f);

        float mouseX = dt * controller.sensivity * input.look.x;
        float mouseY = dt * controller.sensivity * input.look.y;

        camera.cameraYaw += mouseX;
        playerTransform.Rotation = quaternion.RotateY(math.radians(camera.cameraYaw)); ;

        camera.cameraPitch -= mouseY;
        camera.cameraPitch = math.clamp(camera.cameraPitch, -89f, 89f);
        camera.transform.Rotation = math.mul(playerTransform.Rotation, quaternion.RotateX(math.radians(camera.cameraPitch)));

        //Same as below but related to multiplayer it's the same logic but not the same synthax
        if (input.jump.IsSet)
        {
            //  Debug.Log("Jump" + worldName);
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
        //playerInput.jump.started += ctx =>
        //{
        //    physicsVelocity.ValueRW.Linear.y = characterController.ValueRW.jumpForce;
        //    characterController.ValueRW.isGrounded = false;
        //};

        //input.walk.canceled += ctx =>
        //{
        //    characterController.ValueRW.isWalking = false;
        //};
        //input.walk.started += ctx =>
        //{
        //    characterController.ValueRW.isWalking = true;
        //};



    }
}
