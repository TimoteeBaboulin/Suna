using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
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
        Vector2 CharacterMove = actions.Move.ReadValue<Vector2>();
        Vector2 CharacterLook = actions.Look.ReadValue<Vector2>();

        bool isJumpPerfomered = actions.Jump.WasPressedThisFrame();
        bool isWalkPressed = actions.Walk.IsPressed();

        bool isShootPressed = actions.Attack.IsPressed();
        bool isReloadPressed = actions.Reload.WasPressedThisFrame();
        bool isSelectNext = actions.SelectNext.WasPressedThisFrame();
        bool isSelectPrevious = actions.SelectPrevious.WasPressedThisFrame();

        bool isADSPressed = actions.ADS.IsPressed();

        int selectedLocation = 0;
        selectedLocation = actions.SelectMainWeapon.WasPressedThisFrame() ? 1: selectedLocation;
        selectedLocation = actions.SelectSecondWeapon.WasPressedThisFrame() ? 2: selectedLocation;
        selectedLocation = actions.SelectMelee.WasPressedThisFrame() ? 3: selectedLocation;

        foreach (var (controller, input, harvesterActions) in SystemAPI
            .Query<RefRO<CharacterComponent>, RefRW<CharacterInput>, RefRO<PlayerHarvesterActions>>()
            .WithAll<CharacterIsEnable, GhostOwnerIsLocal>()) //GhostOwnerIsLocal clients cannot affect other clients data, can only change this if you're the owner and the local player
        {
            bool plantingOrDefusing = harvesterActions.ValueRO.IsDefusing || harvesterActions.ValueRO.IsPlanting;

            input.ValueRW.move = !plantingOrDefusing ? CharacterMove : new Vector2(0,0);

            input.ValueRW.look = math.radians(CharacterLook * SystemAPI.GetSingleton<ClientSettingsComponent>().Sensivity);

            //TODO :Make these into a function
            if (isJumpPerfomered && !plantingOrDefusing)
            {
                input.ValueRW.jump.Set();
            }
            else
            {
                input.ValueRW.jump = default; //Important to unset or we will have issues down the line
            }

            if (isWalkPressed && !plantingOrDefusing)
            {
                input.ValueRW.walkStarted.Set();
            }
            else
            {
                input.ValueRW.walkStarted = default; //Important to unset or we will have issues down the line
            }

            if (!isWalkPressed && !plantingOrDefusing)
            {
                input.ValueRW.walkCanceled.Set();
            }
            else
            {
                input.ValueRW.walkCanceled = default; //Important to unset or we will have issues down the line
            }

            if (isADSPressed && !plantingOrDefusing)
            {
                input.ValueRW.aimingStarted.Set();
            }
            else
            {
                input.ValueRW.aimingStarted = default; //Important to unset or we will have issues down the line
            }

            if (!isADSPressed && !plantingOrDefusing)
            {
                input.ValueRW.aimingCanceled.Set();
            }
            else
            {
                input.ValueRW.aimingCanceled = default; //Important to unset or we will have issues down the line
            }

            if (isShootPressed && !plantingOrDefusing)
            {
                input.ValueRW.attack.Set();
            }
            else
            {
                input.ValueRW.attack = default;
            }


            if (isReloadPressed && !plantingOrDefusing)
            {
                input.ValueRW.reload.Set();
            }
            else
            {
                input.ValueRW.reload = default;
            }

            if (isSelectNext && !plantingOrDefusing)
            {
                input.ValueRW.selectNext.Set();
            }
            else
            {
                input.ValueRW.selectNext = default;
            }

            if (isSelectPrevious && !plantingOrDefusing)
            {
                input.ValueRW.selectPrevious.Set();
            }
            else
            {
                input.ValueRW.selectPrevious = default;
            }

            input.ValueRW.stuffLocation = selectedLocation;
        }

        foreach (var input in SystemAPI
            .Query<RefRW<CharacterInput>>()
            .WithAll<GhostOwnerIsLocal>()
            .WithNone<CharacterIsEnable>())
        {
            input.ValueRW = default;
        }
    }
}

