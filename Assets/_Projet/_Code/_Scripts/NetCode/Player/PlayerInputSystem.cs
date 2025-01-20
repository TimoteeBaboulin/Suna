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

    private DefaultInputSystem _controls;

    protected override void OnCreate()
    {
        _controls = new DefaultInputSystem();
        _controls.Enable();
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAny<PlayerInputData>();
        RequireForUpdate(GetEntityQuery(builder));
    }

    protected override void OnDestroy()
    {
        _controls.Disable();
    }
    protected override void OnUpdate()
    {
        Vector2 playerMove = _controls.Player.Move.ReadValue<Vector2>();
        Vector2 playerLook = _controls.Player.Look.ReadValue<Vector2>();
        foreach (RefRW<PlayerInputData> input in SystemAPI.Query<RefRW<PlayerInputData>>().WithAll<GhostOwnerIsLocal>()) //GhostOwnerIsLoca clients cannot affect other clients data, can only change this if you're the owner and the local player
        {
            input.ValueRW.move = playerMove;
            input.ValueRW.look = playerLook;
        }
    }
}
