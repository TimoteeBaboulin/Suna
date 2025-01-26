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
        builder.WithAny<PlayerInputData>();
        RequireForUpdate(GetEntityQuery(builder));
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        input.Disable();
    }
    protected override void OnUpdate()
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>(); //IMPORTANT, otherwise some input can be detected multiples time on client and server, thus allowing issues
        InputSystem.Update();
        Vector2 playerMove = actions.Move.ReadValue<Vector2>();
        Vector2 playerLook = actions.Look.ReadValue<Vector2>();
        bool jump = actions.Jump.phase == InputActionPhase.Performed;
        Debug.Log($"Jump detected this frame: {jump}, Phase: {actions.Jump.phase}");
        foreach (RefRW<PlayerInputData> input in SystemAPI.Query<RefRW<PlayerInputData>>().
            WithAll<GhostOwnerIsLocal>()) //GhostOwnerIsLoca clients cannot affect other clients data, can only change this if you're the owner and the local player
        {
            input.ValueRW.move = playerMove;
            input.ValueRW.look = playerLook;

            if (jump)
            {
                input.ValueRW.jump.Set();
            }
            else
            {
                input.ValueRW.jump = default; //Important to unset or we will have issues down the line
            }
        }
    }
}
