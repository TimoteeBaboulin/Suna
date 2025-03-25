using System.Linq;
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
        bool isSelectNext = actions.SelectNext.WasPressedThisFrame();
        bool isSelectPrevious = actions.SelectPrevious.WasPressedThisFrame();

        int selectedId = -1;
        selectedId = actions.SelectMainWeapon.WasPressedThisFrame() ? 0 : selectedId;
        selectedId = actions.SelectSecondWeapon.WasPressedThisFrame() ? 1 : selectedId;
        selectedId = actions.SelectMelee.WasPressedThisFrame() ? 2 : selectedId;

        foreach (var (controller, input, modelBones) in SystemAPI
            .Query<RefRO<CharacterComponent>, RefRW<CharacterInput>, CharacterModelBones>()

            .WithAll<GhostOwnerIsLocal>()) //GhostOwnerIsLocal clients cannot affect other clients data, can only change this if you're the owner and the local player
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
                input.ValueRW.attack.Set();
                input.ValueRW.shootRotation = modelBones.ViewBoneTransform.rotation;
            }
            else
            {
                input.ValueRW.attack = default;
            }


            if (isReloadPressed)
            {
                input.ValueRW.reload.Set();
            }
            else
            {
                input.ValueRW.reload = default;
            }

            if (isSelectNext)
            {
                input.ValueRW.selectNext.Set();
            }
            else
            {
                input.ValueRW.selectNext = default;
            }

            if (isSelectPrevious)
            {
                input.ValueRW.selectPrevious.Set();
            }
            else
            {
                input.ValueRW.selectPrevious = default;
            }

            input.ValueRW.selectStuffId = selectedId;
        }
    }
}
