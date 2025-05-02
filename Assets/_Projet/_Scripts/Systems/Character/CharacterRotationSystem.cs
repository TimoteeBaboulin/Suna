using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
public partial struct CommonCharacterRotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<LocalTransform, CharacterViewRotation>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (characterTransform, characterViewRotation, CharacterInput, characterEntity) in SystemAPI
            .Query<RefRW<LocalTransform>, RefRW<CharacterViewRotation>, RefRO<CharacterInput>>()
            .WithEntityAccess())
        {
            characterTransform.ValueRW.Rotation = quaternion.RotateY(CharacterInput.ValueRO.Yaw);
            characterViewRotation.ValueRW.ViewRotation = quaternion.RotateX(CharacterInput.ValueRO.Pitch);

            float minPitch = -math.PI / 2;
            float maxPitch = math.PI / 2;
            float RemappedValue = Mathf.Clamp((characterViewRotation.ValueRW.Pitch - minPitch) / (maxPitch - minPitch), 0f, 1f);

            AnimationUtils.AddFloatCommand("ViewRotation", RemappedValue, characterEntity, ecb);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
