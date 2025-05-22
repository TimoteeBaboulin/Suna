using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct EnemyOutlineSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityQuery query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ClientComponent>(), ComponentType.ReadOnly<GhostOwnerIsLocal>());
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);

        if (entities.Length == 0) return;

        int OwnerLocalnetworkId = state.EntityManager.GetComponentData<GhostOwner>(entities[0]).NetworkId;
        TeamSideType clientLocalTeamSide = PlayerHelpers.GetPlayerInTeam(OwnerLocalnetworkId);

        Entity localEntity = entities[0];

        foreach (var (outline, ghostOwner, entity) in SystemAPI.Query<RefRW<EnemyOutlineMaterialOverride>, RefRO<GhostOwner>>().WithEntityAccess())
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
                outline.ValueRW.Value = 0; // Removing the enemy outline
            }
            else
            {
                outline.ValueRW.Value = 1; // Adding the enemy outline
            }

            query.Dispose();
        }
    }
}