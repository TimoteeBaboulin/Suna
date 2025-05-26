using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct EnemyOutlineSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityQuery query = state.EntityManager.CreateEntityQuery(
         ComponentType.ReadOnly<ClientComponent>(),
         ComponentType.ReadOnly<GhostOwnerIsLocal>()
     );

        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);

        if (entities.Length == 0)
        {
            entities.Dispose();
            query.Dispose();
            return;
        }

        int OwnerLocalnetworkId = state.EntityManager.GetComponentData<GhostOwner>(entities[0]).NetworkId;
        TeamSideType clientLocalTeamSide = PlayerHelpers.GetPlayerInTeam(OwnerLocalnetworkId);

        Entity localEntity = entities[0];

        foreach (var (outline, tpsModel, ghostOwner, entity) in SystemAPI.Query<
            RefRW<EnemyOutlineMaterialOverride>,
            ThirdPersonCharacterModelReference,
            RefRO<GhostOwner>>().WithEntityAccess())
        {
            if (entity == localEntity) continue;

            TeamSideType teamSide = (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
                ? PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.ValueRO.NetworkId)
                : PlayerHelpers.GetPlayerInTeam(ghostOwner.ValueRO.NetworkId);

            if (teamSide == TeamSideType.Neutre) continue;

            int layer = (clientLocalTeamSide == teamSide) ? 13 : 14;
            Color outlineColor = (clientLocalTeamSide == teamSide) ? Color.cyan : Color.magenta;

            tpsModel.ModelGameObject.layer = layer;
            var renderer = tpsModel.ModelGameObject.GetComponentInChildren<SkinnedMeshRenderer>();

            if (renderer.materials.Length > 1)
            {
                renderer.materials[1].SetColor("_OutlineColor", outlineColor);
            }
        }

        entities.Dispose();
        query.Dispose();
    }
}