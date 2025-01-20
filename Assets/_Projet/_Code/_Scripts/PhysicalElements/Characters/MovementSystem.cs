using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

public partial struct MovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        InputAction zqsd = InputSystem.actions.FindAction("Move");
        InputAction look = InputSystem.actions.FindAction("Look");

        foreach (var (characterController, localTransform, physicsVelocity, cameraAttach)
            in SystemAPI.Query<RefRO<CharacterControllerComponent>, RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRW<CameraAttachComponent>>())
        {
            ref readonly CharacterControllerComponent controller = ref characterController.ValueRO;
            ref LocalTransform playerTransform = ref localTransform.ValueRW;
            ref PhysicsVelocity vel = ref physicsVelocity.ValueRW;
            ref CameraAttachComponent camera = ref cameraAttach.ValueRW;

            float x = zqsd.ReadValue<Vector2>().x;
            float z = zqsd.ReadValue<Vector2>().y;

            float3 move = controller.maxRunningSpeed * SystemAPI.Time.DeltaTime * (x * playerTransform.Right() + z * playerTransform.Forward());
            move.y = vel.Linear.y;
            vel.Linear = move;

            camera.transform.Position = playerTransform.Position;

            float mouseX = SystemAPI.Time.DeltaTime * controller.sensivity * look.ReadValue<Vector2>().x;
            float mouseY = SystemAPI.Time.DeltaTime * controller.sensivity * look.ReadValue<Vector2>().y;

            camera.cameraYaw += mouseX;
            playerTransform.Rotation = quaternion.RotateY(math.radians(camera.cameraYaw)); ;

            camera.cameraPitch -= mouseY;
            camera.cameraPitch = math.clamp(camera.cameraPitch, -89f, 89f);
            camera.transform.Rotation = math.mul(playerTransform.Rotation, quaternion.RotateX(math.radians(camera.cameraPitch)));
        }
    }
}
