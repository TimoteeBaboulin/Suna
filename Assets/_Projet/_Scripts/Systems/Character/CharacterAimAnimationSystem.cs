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

            if (stuffName == "KnifeNeutral"
                || stuffName == "LP-17"
                || stuffName == "FAKIR")
            {
                AnimationUtils.AddBoolCommand("AimHandgun", true, characterEntity, ecb);

                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb);
            }
            else if (stuffName == "Decimator"
                || stuffName == "SKAR-18")
            {
                AnimationUtils.AddBoolCommand("AimRifle", true, characterEntity, ecb);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb);
            }
            else if (stuffName == "Banduka")
            {
                AnimationUtils.AddBoolCommand("AimShotgun", true, characterEntity, ecb);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb);
            }
            else if (stuffName == "Nelara")
            {
                AnimationUtils.AddBoolCommand("AimPM", true, characterEntity, ecb);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb);
            }
            else if (stuffName == "Laksya")
            {
                AnimationUtils.AddBoolCommand("AimSniper", true, characterEntity, ecb);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb);
            }
            else if (stuffName == "Harvester")
            {
                AnimationUtils.AddBoolCommand("AimHarvester", true, characterEntity, ecb);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb);
            }
            else if (stuffName == "HEGrenade" 
                || stuffName == "Flashbang")
            {
                AnimationUtils.AddBoolCommand("AimHEGrenade", true, characterEntity, ecb);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb);
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
