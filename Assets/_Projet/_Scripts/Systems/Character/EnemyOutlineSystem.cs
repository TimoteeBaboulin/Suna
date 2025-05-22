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
        EntityQuery query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ClientComponent>(), ComponentType.ReadOnly<GhostOwnerIsLocal>());
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);

        if (entities.Length == 0) return;

        int OwnerLocalnetworkId = state.EntityManager.GetComponentData<GhostOwner>(entities[0]).NetworkId;
        TeamSideType clientLocalTeamSide = PlayerHelpers.GetPlayerInTeam(OwnerLocalnetworkId);

        Entity localEntity = entities[0];

        foreach (var (outline, tpsModel, ghostOwner, entity) in SystemAPI.Query<RefRW<EnemyOutlineMaterialOverride>, ThirdPersonCharacterModelReference, RefRO<GhostOwner>>().WithEntityAccess())
        {
            if (entity == localEntity) continue;

            TeamSideType teamSide;
            if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
            {
                teamSide = PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.ValueRO.NetworkId);
            }
            else
            {
                teamSide = PlayerHelpers.GetPlayerInTeam(ghostOwner.ValueRO.NetworkId);
            }

            if (teamSide == TeamSideType.Neutre) continue;

            if (clientLocalTeamSide == teamSide)
            {
                tpsModel.ModelGameObject.layer = 13; // Visibility through walls is managed just by using that layer
                tpsModel.ModelGameObject.GetComponentInChildren<SkinnedMeshRenderer>().materials[1].SetColor("_OutlineColor", Color.cyan);
            }
            else
            {
                tpsModel.ModelGameObject.layer = 14; // Enemy outline layer
                tpsModel.ModelGameObject.GetComponentInChildren<SkinnedMeshRenderer>().materials[1].SetColor("_OutlineColor", Color.magenta);
            }

            query.Dispose();
        }
    }
}