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
       // builder.WithAll<PlayerData, PlayerInputData, LocalTransform>();
        builder.WithAll<CharacterControllerComponent, LocalTransform, PhysicsVelocity, CameraAttachComponent>();
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

//[BurstCompile]
//public partial struct PlayerMovementJob : IJobEntity
//{
//    public float dt;

//    public void Execute(PlayerData player, PlayerInputData input, ref LocalTransform transform)
//    {
//        float3 movement = new float3(input.move.x, 0, input.move.y) * player.speed * dt;
//        transform.Position = transform.Translate(movement).Position;
//    }
//}


//[BurstCompile]
//[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
//public partial struct PlayerMovementSystem : ISystem
//{
//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
//        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
//        // builder.WithAll<PlayerData, PlayerInputData, LocalTransform>();
//        builder.WithAll<RefRO<CharacterControllerComponent>, RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRW<CameraAttachComponent>>();
//        state.RequireForUpdate(state.GetEntityQuery(builder));
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        PlayerMovementJob job = new PlayerMovementJob
//        {
//            dt = SystemAPI.Time.DeltaTime
//        };
//        state.Dependency = job.ScheduleParallel(state.Dependency);
//    }
//}

[BurstCompile]
public partial struct PlayerMovementJob : IJobEntity
{
    public float dt;

    public void Execute(RefRO<CharacterControllerComponent> characterController, ref LocalTransform localTransform, ref CameraAttachComponent cameraAttach, ref PhysicsVelocity physicsVelocity)
    {
        //float3 movement = new float3(input.move.x, 0, input.move.y) * player.speed * dt;
        //localTransform.Position = localTransform.Translate(movement).Position;

        InputAction zqsd = InputSystem.actions.FindAction("Move");
        InputAction look = InputSystem.actions.FindAction("Look");

        ref readonly CharacterControllerComponent controller = ref characterController.ValueRO;
        ref LocalTransform playerTransform = ref localTransform;
        ref PhysicsVelocity vel = ref physicsVelocity;
        ref CameraAttachComponent camera = ref cameraAttach;

        float x = zqsd.ReadValue<Vector2>().x;
        float z = zqsd.ReadValue<Vector2>().y;

        float3 move = controller.maxRunningSpeed * dt * (x * playerTransform.Right() + z * playerTransform.Forward());
        move.y = vel.Linear.y;
        vel.Linear = move;

        camera.transform.Position = playerTransform.Position;

        float mouseX = dt * controller.sensivity * look.ReadValue<Vector2>().x;
        float mouseY = dt * controller.sensivity * look.ReadValue<Vector2>().y;

        camera.cameraYaw += mouseX;
        playerTransform.Rotation = quaternion.RotateY(math.radians(camera.cameraYaw)); ;

        camera.cameraPitch -= mouseY;
        camera.cameraPitch = math.clamp(camera.cameraPitch, -89f, 89f);
        camera.transform.Rotation = math.mul(playerTransform.Rotation, quaternion.RotateX(math.radians(camera.cameraPitch)));
    }
}
