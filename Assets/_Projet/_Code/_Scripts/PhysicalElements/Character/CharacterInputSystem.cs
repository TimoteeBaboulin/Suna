using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class CharacterInputSystem : SystemBase
{
    public DefaultInputSystem input;

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

        int selectedLocation = 0;
        selectedLocation = actions.SelectMainWeapon.WasPressedThisFrame() ? 1: selectedLocation;
        selectedLocation = actions.SelectSecondWeapon.WasPressedThisFrame() ? 2: selectedLocation;
        selectedLocation = actions.SelectMelee.WasPressedThisFrame() ? 3: selectedLocation;

        

        foreach (var (controller, input, characterCamera) in SystemAPI
            .Query<RefRO<CharacterComponent>, RefRW<CharacterInput>, RefRO<CharacterCameraComponent>>()

            .WithAll<GhostOwnerIsLocal>()) //GhostOwnerIsLocal clients cannot affect other clients data, can only change this if you're the owner and the local player
        {

            input.ValueRW.move = input.ValueRO.enabled ? CharacterMove : new Vector2(0,0);

            input.ValueRW.look = input.ValueRO.enabled ? CharacterLook * SystemAPI.GetSingleton<ClientSettingsComponent>().Sensivity : new Vector2(0, 0);

            //TODO :Make these into a function
            if (isJumpPerfomered && input.ValueRO.enabled)
            {
                input.ValueRW.jump.Set();
            }
            else
            {
                input.ValueRW.jump = default; //Important to unset or we will have issues down the line
            }

            if (isWalkStarted && input.ValueRO.enabled)
            {
                input.ValueRW.walkStarted.Set();
            }
            else
            {
                input.ValueRW.walkStarted = default; //Important to unset or we will have issues down the line
            }

            if (isWalkCanceled && input.ValueRO.enabled)
            {
                input.ValueRW.walkCanceled.Set();
            }
            else
            {
                input.ValueRW.walkCanceled = default; //Important to unset or we will have issues down the line
            }

            if (isShootPressed && input.ValueRO.enabled)
            {
                input.ValueRW.attack.Set();
                input.ValueRW.shootRotation = Camera.main.transform.rotation;
            }
            else
            {
                input.ValueRW.attack = default;
            }


            if (isReloadPressed && input.ValueRO.enabled)
            {
                input.ValueRW.reload.Set();
            }
            else
            {
                input.ValueRW.reload = default;
            }

            if (isSelectNext && input.ValueRO.enabled)
            {
                input.ValueRW.selectNext.Set();
            }
            else
            {
                input.ValueRW.selectNext = default;
            }

            if (isSelectPrevious && input.ValueRO.enabled)
            {
                input.ValueRW.selectPrevious.Set();
            }
            else
            {
                input.ValueRW.selectPrevious = default;
            }

            input.ValueRW.stuffLocation = selectedLocation;
        }
    }
}

