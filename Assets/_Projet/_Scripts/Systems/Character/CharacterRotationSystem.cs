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

            //quaternion newRotation = quaternion.identity;
            //newRotation = math.mul(newRotation, characterTransform.ValueRO.Rotation);
            //newRotation = math.mul(newRotation, characterViewRotation.ValueRO.ViewRotation);

            //newRotation = math.mul(newRotation, quaternion.RotateY(mouseX));
            //newRotation = math.mul(newRotation, quaternion.RotateX(-mouseY));

            characterTransform.ValueRW.Rotation = math.mul(characterTransform.ValueRO.Rotation, quaternion.RotateY(mouseX));
            characterViewRotation.ValueRW.ViewRotation = math.mul(characterViewRotation.ValueRO.ViewRotation, quaternion.RotateX(-mouseY));

            //characterTransform.ValueRW.Rotation = math.mul(characterTransform.ValueRO.Rotation, quaternion.RotateY(math.radians(mouseX)));

            // We perform the calculations in degrees to prevent the clamp from returning the opposite value if the mouse movement value is too high.
            //float newViewRotationDeg = math.degrees(characterViewRotation.ValueRW.ViewRotation.value.x) - mouseY;
            //newViewRotationDeg = math.clamp(newViewRotationDeg, -45, 45);
            //characterViewRotation.ValueRW.ViewRotation = math.mul(characterViewRotation.ValueRO.ViewRotation, quaternion.RotateX(-mouseY / 4));
        }
    }
}
