using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct ServerCharacterAimAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffDatabaseAccess>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (stuffList, stuffInfo, characterEntity) in SystemAPI
            .Query<DynamicBuffer<CharacterStuffList>, RefRO<CharacterStuffInfos>>()
            .WithEntityAccess())
        {
            Entity stuffInHand = StuffUtils.GetStuffInHand(stuffList, stuffInfo.ValueRO);

            if (stuffInHand == Entity.Null) continue;

            if (!state.EntityManager.HasComponent<StuffDatabaseAccess>(stuffInHand)) continue;

            FixedString128Bytes stuffName = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffInHand).NameInDatabase;

            if (stuffName == "KnifeNeutral")
            {
                AnimationUtils.AddBoolCommand("AimKnife", true, characterEntity, ecb);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb);
            }
            else if (stuffName == "LP-17"
                || stuffName == "FAKIR")
            {
                AnimationUtils.AddBoolCommand("AimHandgun", true, characterEntity, ecb);

                AnimationUtils.AddBoolCommand("AimKnife", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb);
            }
            else if (stuffName == "Banduka"
                || stuffName == "Decimator"
                || stuffName == "SKAR-18")
            {
                AnimationUtils.AddBoolCommand("AimRifle", true, characterEntity, ecb);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimKnife", false, characterEntity, ecb);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
