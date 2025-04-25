using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct DropStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterStuffList>();
        state.RequireForUpdate<CharacterInput>();
        state.RequireForUpdate<UnequipStuffQueue>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Get ECB
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var unequipStuffQueue = SystemAPI.GetSingletonBuffer<UnequipStuffQueue>();

        foreach (var (stuffListRW, inputRO, transformRO, shootStartPosDeltaRO, viewRO, chara) in SystemAPI
        .Query<RefRW<CharacterStuffList>, RefRO<CharacterInput>, RefRO<LocalTransform>, RefRO<CharacterShootStartPositionDelta>, RefRO<CharacterViewRotation>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRO.ValueRO;
            ref CharacterStuffList stuffList = ref stuffListRW.ValueRW;
            if (input.drop.IsSet && stuffList.StuffInHandSlot != StuffSlot.Melee)
            {
                unequipStuffQueue.Add(new UnequipStuffQueue
                {
                    Stuff = stuffList.StuffInHand,
                    Owner = chara,
                });

                Entity dropedStuffPrefab = SystemAPI.GetComponent<StuffDynamicData>(stuffList.StuffInHand).dropedEntityPrefab;
                Entity dropedStuff = ecb.Instantiate(dropedStuffPrefab);

                ecb.SetComponent(dropedStuff, new StuffEntityInHandRef { Value = stuffList.StuffInHand });

                float3 startPosition = shootStartPosDeltaRO.ValueRO.PositionDelta + transformRO.ValueRO.Position;
                quaternion shootRotation = math.mul(transformRO.ValueRO.Rotation, viewRO.ValueRO.ViewRotation);
                float3 forward = math.mul(shootRotation, math.forward());

                ecb.SetComponent(dropedStuff, new LocalTransform
                {
                    Position = startPosition + forward,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });

                ecb.SetComponent(dropedStuff, new PhysicsVelocity
                {
                    Linear = forward * 5f,
                    Angular = float3.zero
                });
            }
        }
    }
}