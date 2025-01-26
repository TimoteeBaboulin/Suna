using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class PlayerInputSystem : SystemBase
{
    //private ControlsTemp _controls;

    private DefaultInputSystem input;

    DefaultInputSystem.PlayerActions actions;

    protected override void OnCreate()
    {
        input = new DefaultInputSystem();
        input.Enable();
        actions = input.Player;
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAny<PlayerInput>();
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
        Vector2 playerMove = actions.Move.ReadValue<Vector2>();
        Vector2 playerLook = actions.Look.ReadValue<Vector2>();

        bool isJumpPerfomered = actions.Jump.phase == InputActionPhase.Performed;
        bool isWalkStarted = actions.Walk.phase == InputActionPhase.Started;
        bool isWalkCanceled = actions.Walk.phase == InputActionPhase.Canceled;
        foreach (RefRW<PlayerInput> input in SystemAPI.Query<RefRW<PlayerInput>>().
            WithAll<GhostOwnerIsLocal>()) //GhostOwnerIsLoca clients cannot affect other clients data, can only change this if you're the owner and the local player
        {
            input.ValueRW.move = playerMove;
            input.ValueRW.look = playerLook;
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
        }
    }
}
