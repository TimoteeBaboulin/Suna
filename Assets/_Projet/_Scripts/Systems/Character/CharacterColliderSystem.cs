using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
partial struct CommonCharacterColliderSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterColliderComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var(characterCollider, ghostOwner, entity) in SystemAPI
            .Query<RefRW<CharacterColliderComponent>, RefRO<GhostOwner>>()
            .WithAll<ThirdPersonCharacterModelBonesTransform, CharacterColliderInitEntityTag>()
            .WithEntityAccess())
        {
            if (!SystemAPI.TryGetSingleton(out ClientPrefabData prefabsData)) { continue; }
            TeamSideType teamSide;
            if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
            {
                teamSide = PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.ValueRO.NetworkId);
            }
            else
            {
                teamSide = PlayerHelpers.GetPlayerInTeam(ghostOwner.ValueRO.NetworkId);
            }

            Debug.Log($"[CommonCharacterColliderSystem]TeamSide {teamSide}");
            switch (teamSide)
            {
                case TeamSideType.Corpo:
                    CharacterColliderUtils.InstantiateCorpoCollider(characterCollider, entity, prefabsData, ecb, state.EntityManager);
                    ecb.RemoveComponent<CharacterColliderInitEntityTag>(entity);
                    break;
                case TeamSideType.Natif:
                    CharacterColliderUtils.InstantiateNatifCollider(characterCollider, entity, prefabsData, ecb, state.EntityManager);
                    ecb.RemoveComponent<CharacterColliderInitEntityTag>(entity);
                    break;
            }
        }

        foreach (var (characterCollider, modelBones) in SystemAPI
            .Query<RefRO<CharacterColliderComponent>, ThirdPersonCharacterModelBonesTransform>()
            .WithNone<CharacterColliderInitEntityTag>())
        {
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.HeadEntity, modelBones.HeadBoneTransform, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.ArmLeftEntity0, modelBones.ArmLeftBoneTransform0, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.ArmLeftEntity1, modelBones.ArmLeftBoneTransform1, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.ArmLeftEntity2, modelBones.ArmLeftBoneTransform2, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.ArmRightEntity0, modelBones.ArmRightBoneTransform0, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.ArmRightEntity1, modelBones.ArmRightBoneTransform1, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.ArmRightEntity2, modelBones.ArmRightBoneTransform2, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.ThoraxEntity, modelBones.ThoraxBoneTransform, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.StomachEntity0, modelBones.StomachBoneTransform0, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.StomachEntity1, modelBones.StomachBoneTransform1, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.LegLeftEntity0, modelBones.LegLeftBoneTransform0, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.LegLeftEntity1, modelBones.LegLeftBoneTransform1, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.LegLeftEntity2, modelBones.LegLeftBoneTransform2, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.LegRightEntity0, modelBones.LegRightBoneTransform0, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.LegRightEntity1, modelBones.LegRightBoneTransform1, ecb, state.EntityManager);
            CharacterColliderUtils.SetTransform(characterCollider.ValueRO.LegRightEntity2, modelBones.LegRightBoneTransform2, ecb, state.EntityManager);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
