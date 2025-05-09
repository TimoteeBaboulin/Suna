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

        foreach (var (stuffList, stuffInfo, ghostOwner, characterEntity) in SystemAPI
            .Query<DynamicBuffer<CharacterStuffList>, RefRO<CharacterStuffInfos>, RefRO<GhostOwner>>()
            .WithEntityAccess())
        {
            Entity stuffInHand = StuffUtils.GetStuffInHand(stuffList, stuffInfo.ValueRO);

            if (stuffInHand == Entity.Null) continue;

            if (!state.EntityManager.HasComponent<StuffDatabaseAccess>(stuffInHand)) continue;

            FixedString128Bytes stuffName = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffInHand).NameInDatabase;

            int networkId = ghostOwner.ValueRO.NetworkId;

            if (stuffName == "KnifeNeutral"
                || stuffName == "LP-17"
                || stuffName == "FAKIR")
            {
                AnimationUtils.AddBoolCommand("AimHandgun", true, characterEntity, ecb, networkId);

                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb, networkId);
            }
            else if (stuffName == "Decimator"
                || stuffName == "SKAR-18")
            {
                AnimationUtils.AddBoolCommand("AimRifle", true, characterEntity, ecb, networkId);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb, networkId);
            }
            else if (stuffName == "Banduka")
            {
                AnimationUtils.AddBoolCommand("AimShotgun", true, characterEntity, ecb, networkId);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb, networkId);
            }
            else if (stuffName == "Nelara")
            {
                AnimationUtils.AddBoolCommand("AimPM", true, characterEntity, ecb, networkId);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb, networkId);
            }
            else if (stuffName == "Laksya")
            {
                AnimationUtils.AddBoolCommand("AimSniper", true, characterEntity, ecb, networkId);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb, networkId);
            }
            else if (stuffName == "Harvester")
            {
                AnimationUtils.AddBoolCommand("AimHarvester", true, characterEntity, ecb, networkId);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHEGrenade", false, characterEntity, ecb, networkId);
            }
            else if (stuffName == "HEGrenade" 
                || stuffName == "Flashbang")
            {
                AnimationUtils.AddBoolCommand("AimHEGrenade", true, characterEntity, ecb, networkId);

                AnimationUtils.AddBoolCommand("AimHandgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimShotgun", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimRifle", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimPM", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimSniper", false, characterEntity, ecb, networkId);
                AnimationUtils.AddBoolCommand("AimHarvester", false, characterEntity, ecb, networkId);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
