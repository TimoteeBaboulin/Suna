using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class CharacterInputSystem : SystemBase
{
    private DefaultInputSystem input;

    DefaultInputSystem.PlayerActions actions;

    protected override void OnCreate()
    {
        input = new DefaultInputSystem();
        input.Enable();
        actions = input.Player;
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAny<CharacterInput>();
        RequireForUpdate(GetEntityQuery(builder));
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        input.Disable();
    }
    protected override void OnUpdate()
    {
       // InputSystem.Update();
        Vector2 CharacterMove = actions.Move.ReadValue<Vector2>();
        Vector2 CharacterLook = actions.Look.ReadValue<Vector2>();

        bool isJumpPerfomered = actions.Jump.WasPressedThisFrame();
        bool isWalkStarted = actions.Walk.phase == InputActionPhase.Started;
        bool isWalkCanceled = actions.Walk.phase == InputActionPhase.Canceled;
        bool isShootPressed = actions.Attack.IsPressed();
        bool isReloadPressed = actions.Reload.WasPressedThisFrame();

        foreach (var (controller, input) in SystemAPI
            .Query<RefRO<CharacterComponent>, RefRW<CharacterInput>>()
            .WithAll<GhostOwnerIsLocal>()) //GhostOwnerIsLoca clients cannot affect other clients data, can only change this if you're the owner and the local player
        {
            input.ValueRW.move = CharacterMove;

            input.ValueRW.look = CharacterLook * SystemAPI.GetSingleton<ClientSettingsComponent>().Sensivity;

            //TODO :Make these into a function
            if (isJumpPerfomered)
            {
                input.ValueRW.jump.Set();
            }
            else
            {
                input.ValueRW.jump = default; //Important to unset or we will have issues down the line
            }

            if (isWalkStarted)
            {
                input.ValueRW.walkStarted.Set();
            }
            else
            {
                input.ValueRW.walkStarted = default; //Important to unset or we will have issues down the line
            }

            if (isWalkCanceled)
            {
                input.ValueRW.walkCanceled.Set();
            }
            else
            {
                input.ValueRW.walkCanceled = default; //Important to unset or we will have issues down the line
            }

            if (isShootPressed)
            {
                input.ValueRW.shoot.Set();

                input.ValueRW.shootTransform = new LocalTransform
                {
                    Position = MainGameObjectCamera.Instance.transform.position,
                    Rotation = MainGameObjectCamera.Instance.transform.rotation,
                    Scale = 1f,
                };
            }
            else
            {
                input.ValueRW.shoot = default;
            }

            if (isReloadPressed)
            {
                input.ValueRW.reload.Set();
            }
            else
            {
                input.ValueRW.reload = default;
            }
        }
    }
}
