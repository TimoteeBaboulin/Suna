using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct CharacterDebugInput : IInputComponentData
{
    [GhostField] public InputEvent Respawn;
}


[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class DebugInputSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<CharacterDebugInput>();
    }

    protected override void OnUpdate()
    {
        foreach (var characterDebugInput in SystemAPI
            .Query<RefRW<CharacterDebugInput>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                characterDebugInput.ValueRW.Respawn.Set();
            }
            else
            {
                characterDebugInput.ValueRW.Respawn = default;
            }
        }
    }
}


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct CharacterInputDebugSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterDebugInput>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (characterDebugInput, currentHeath) in SystemAPI
            .Query<RefRO<CharacterDebugInput>, RefRW<CurrentHealthComponent>>()
            .WithAll<CharacterComponent>())
        {
            if (characterDebugInput.ValueRO.Respawn.IsSet)
            {
                currentHeath.ValueRW.Value = 0;
            }
        }
    }
}
