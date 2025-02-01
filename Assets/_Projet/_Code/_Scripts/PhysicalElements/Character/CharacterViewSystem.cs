using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(CharacterMovementSystem))]
partial struct CharacterLookSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MainEntityCameraTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        if (!networkTime.IsFirstPredictionTick)
        {
            return;
        }

        foreach (var (transform, parent) in SystemAPI
            .Query<RefRW<LocalTransform>, RefRO<Parent>>()
            .WithAll<MainEntityCameraTag>())
        {
            RefRW<LocalTransform> characterTransform = SystemAPI.GetComponentRW<LocalTransform>(parent.ValueRO.Value);
            RefRO<CharacterInput> input = SystemAPI.GetComponentRO<CharacterInput>(parent.ValueRO.Value);

            float mouseX = SystemAPI.Time.DeltaTime * input.ValueRO.look.x;
            float mouseY = SystemAPI.Time.DeltaTime * input.ValueRO.look.y;

            characterTransform.ValueRW.Rotation = math.mul(characterTransform.ValueRO.Rotation, quaternion.RotateY(math.radians(mouseX)));
            transform.ValueRW.Rotation = math.mul(transform.ValueRO.Rotation, quaternion.RotateX(math.radians(-mouseY)));

            //transform.ValueRW.Rotation.value.x = math.clamp(transform.ValueRO.Rotation.value.x, math.radians(-89f), math.radians(89f));
        }
    }
}
