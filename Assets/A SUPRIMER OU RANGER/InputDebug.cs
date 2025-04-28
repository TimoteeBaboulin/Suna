using Unity.Collections;
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
        foreach (var (characterDebugInput, entity) in SystemAPI
            .Query<RefRW<CharacterDebugInput>>()
            .WithAll<GhostOwnerIsLocal>().WithEntityAccess())
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
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (characterDebugInput, currentHeath, entity) in SystemAPI
            .Query<RefRO<CharacterDebugInput>, RefRW<CurrentHealthComponent>>()
            .WithAll<CharacterComponent>().WithEntityAccess())
        {
            if (characterDebugInput.ValueRO.Respawn.IsSet)
            {
                currentHeath.ValueRW.Value = 0;
                ecb.AddComponent<HasNoHealthTag>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
