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

            float3 move = (playerTransform.Right() * x + playerTransform.Forward() * z) * controller.maxRunningSpeed * SystemAPI.Time.DeltaTime;
            move.y = vel.Linear.y;
            vel.Linear = move;

            camera.transform.Position = playerTransform.Position;

            float mouseX = look.ReadValue<Vector2>().x;
            float mouseY = look.ReadValue<Vector2>().y;

            quaternion playerNewRotation = math.mul(quaternion.Euler(0, mouseX, 0), playerTransform.Rotation);
            playerTransform.Rotation = playerNewRotation;

            quaternion cameraNewRotation = math.mul(quaternion.Euler(-mouseY, mouseX, 0), camera.transform.Rotation);
            camera.transform.Rotation = cameraNewRotation;
        }
    }
}
