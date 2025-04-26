using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class CharacterColliderUtils
{
    public static void InstantiateCollider(ref Entity characterCollider,
        in Entity prefab, in Entity character, in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        Entity bodyPartEntity = entityManager.Instantiate(prefab);
        if (entityManager.HasComponent<CharacterColliderDataComponent>(bodyPartEntity))
        {
            var dataComponent = entityManager.GetComponentData<CharacterColliderDataComponent>(bodyPartEntity);
            dataComponent.CharacterEntity = character;
            ecb.SetComponent(bodyPartEntity, dataComponent);
        }
        ecb.AppendToBuffer(character, new LinkedEntityGroup { Value = bodyPartEntity });
        characterCollider = bodyPartEntity;
    }

    public static void InstantiateCorpoCollider(RefRW<CharacterColliderComponent> characterCollider, in Entity entity, in ClientPrefabData prefabsData, 
        in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        InstantiateCollider(ref characterCollider.ValueRW.HeadEntity,prefabsData.CorpoHeadCollider, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmLeftEntity0,prefabsData.CorpoArmCollider0, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmLeftEntity1,prefabsData.CorpoArmCollider1, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmLeftEntity2,prefabsData.CorpoArmCollider2, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmRightEntity0,prefabsData.CorpoArmCollider0, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmRightEntity1,prefabsData.CorpoArmCollider1, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmRightEntity2,prefabsData.CorpoArmCollider2, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ThoraxEntity,prefabsData.CorpoThoraxCollider, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.StomachEntity0,prefabsData.CorpoStomachCollider0, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.StomachEntity1,prefabsData.CorpoStomachCollider1, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegLeftEntity0,prefabsData.CorpoLegCollider0, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegLeftEntity1,prefabsData.CorpoLegCollider1, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegLeftEntity2,prefabsData.CorpoLegCollider2, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegRightEntity0,prefabsData.CorpoLegCollider0, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegRightEntity1,prefabsData.CorpoLegCollider1, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegRightEntity2, prefabsData.CorpoLegCollider2, entity, ecb, entityManager);
    }

    public static void InstantiateNatifCollider(RefRW<CharacterColliderComponent> characterCollider, in Entity entity, in ClientPrefabData prefabsData,
        in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        InstantiateCollider(ref characterCollider.ValueRW.HeadEntity, prefabsData.NatifHeadCollider, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmLeftEntity0, prefabsData.NatifArmCollider0, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmLeftEntity1, prefabsData.NatifArmCollider1, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmLeftEntity2, prefabsData.NatifArmCollider2, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmRightEntity0, prefabsData.NatifArmCollider0, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmRightEntity1, prefabsData.NatifArmCollider1, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ArmRightEntity2, prefabsData.NatifArmCollider2, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.ThoraxEntity, prefabsData.NatifThoraxCollider, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.StomachEntity0, prefabsData.NatifStomachCollider0, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.StomachEntity1, prefabsData.NatifStomachCollider1, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegLeftEntity0, prefabsData.NatifLegCollider0, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegLeftEntity1, prefabsData.NatifLegCollider1, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegLeftEntity2, prefabsData.NatifLegCollider2, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegRightEntity0, prefabsData.NatifLegCollider0, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegRightEntity1, prefabsData.NatifLegCollider1, entity, ecb, entityManager);
        InstantiateCollider(ref characterCollider.ValueRW.LegRightEntity2, prefabsData.NatifLegCollider2, entity, ecb, entityManager);
    }

    public static void SetTransform(in Entity collider, in Transform bone, in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        if (entityManager.HasComponent<LocalTransform>(collider))
        {
            LocalTransform bodyPartTransform = entityManager.GetComponentData<LocalTransform>(collider);
            bodyPartTransform.Position = bone.position;
            bodyPartTransform.Rotation = bone.rotation;
            ecb.SetComponent(collider, bodyPartTransform);
        }
    }
}
