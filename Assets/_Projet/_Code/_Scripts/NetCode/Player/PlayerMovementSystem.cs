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
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<PlayerInputData, CharacterControllerComponent, LocalTransform, PhysicsVelocity, CameraAttachComponent>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        PlayerMovementJob job = new PlayerMovementJob
        {
            dt = SystemAPI.Time.DeltaTime
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct PlayerMovementJob : IJobEntity
{
    public float dt;

    public void Execute(ref PlayerInputData playerInput,RefRO<CharacterControllerComponent> characterController, ref LocalTransform localTransform, ref CameraAttachComponent cameraAttach, ref PhysicsVelocity physicsVelocity)
    {
        ref readonly CharacterControllerComponent controller = ref characterController.ValueRO;
        ref LocalTransform playerTransform = ref localTransform;
        ref PhysicsVelocity vel = ref physicsVelocity;
        ref CameraAttachComponent camera = ref cameraAttach;

        float x = playerInput.move.x;
        float z = playerInput.move.y;

        float3 move = controller.maxRunningSpeed * dt * (x * playerTransform.Right() + z * playerTransform.Forward());
        move.y = vel.Linear.y;
        vel.Linear = move;

        camera.transform.Position = playerTransform.Position;

        float mouseX = dt * controller.sensivity * playerInput.look.x;
        float mouseY = dt * controller.sensivity * playerInput.look.y;

        camera.cameraYaw += mouseX;
        playerTransform.Rotation = quaternion.RotateY(math.radians(camera.cameraYaw)); ;

        camera.cameraPitch -= mouseY;
        camera.cameraPitch = math.clamp(camera.cameraPitch, -89f, 89f);
        camera.transform.Rotation = math.mul(playerTransform.Rotation, quaternion.RotateX(math.radians(camera.cameraPitch)));
    }
}
