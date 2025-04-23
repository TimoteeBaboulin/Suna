using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

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

        foreach (var (characterCollider, entity) in SystemAPI
            .Query<RefRW<CharacterColliderComponent>>()
            .WithAll<CharacterColliderInitEntityTag>()
            .WithEntityAccess())
        {
            if (!SystemAPI.TryGetSingleton(out ClientPrefabData prefabsData)) { continue; }

            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.HeadEntity,
                prefabsData.CorpoHeadCollider, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.ArmLeftEntity0,
                prefabsData.CorpoArmCollider0, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.ArmLeftEntity1,
                prefabsData.CorpoArmCollider1, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.ArmLeftEntity2,
                prefabsData.CorpoArmCollider2, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.ArmRightEntity0,
                prefabsData.CorpoArmCollider0, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.ArmRightEntity1,
                prefabsData.CorpoArmCollider1, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.ArmRightEntity2,
                prefabsData.CorpoArmCollider2, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.ThoraxEntity,
                prefabsData.CorpoThoraxCollider, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.StomachEntity0,
                prefabsData.CorpoStomachCollider0, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.StomachEntity1,
                prefabsData.CorpoStomachCollider1, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.LegLeftEntity0,
                prefabsData.CorpoLegCollider0, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.LegLeftEntity1,
                prefabsData.CorpoLegCollider1, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.LegLeftEntity2,
                prefabsData.CorpoLegCollider2, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.LegRightEntity0,
                prefabsData.CorpoLegCollider0, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.LegRightEntity1,
                prefabsData.CorpoLegCollider1, entity, ecb, state.EntityManager);
            CharacterColliderUtils.InstantiateCollider(ref characterCollider.ValueRW.LegRightEntity2,
                prefabsData.CorpoLegCollider2, entity, ecb, state.EntityManager);

            ecb.RemoveComponent<CharacterColliderInitEntityTag>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (characterCollider, modelBones) in SystemAPI
            .Query<RefRO<CharacterColliderComponent>, ThirdPersonCharacterModelBonesTransform>())
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
