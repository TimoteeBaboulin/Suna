using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

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
        foreach (var (characterTransform, characterViewRotation, CharacterInput) in SystemAPI
            .Query<RefRW<LocalTransform>, RefRW<CharacterViewRotation>, RefRO<CharacterInput>>())
        {
            float mouseX = CharacterInput.ValueRO.look.x * SystemAPI.Time.DeltaTime;
            float mouseY = CharacterInput.ValueRO.look.y * SystemAPI.Time.DeltaTime;

            characterTransform.ValueRW.Rotation = math.mul(characterTransform.ValueRO.Rotation, quaternion.RotateY(mouseX));

            characterViewRotation.ValueRW.Pitch += math.degrees(-mouseY);
            characterViewRotation.ValueRW.Pitch = math.clamp(characterViewRotation.ValueRW.Pitch, -89f, 89f);
            characterViewRotation.ValueRW.ViewRotation.value = quaternion.RotateX(math.radians(characterViewRotation.ValueRO.Pitch)).value;
        }
    }
}
