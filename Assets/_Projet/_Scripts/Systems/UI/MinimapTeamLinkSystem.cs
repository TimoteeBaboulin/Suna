using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct MinimapTeamLinkSystem : ISystem
{
    public class MinimapTeamArgs : EventArgs
    {
        public int TeamId;
        public int PlayerId;
        public float3 Position;
        public float3 Forward;
        public bool Alive;
    }
    public static EventHandler<MinimapTeamArgs> OnMinimapTeamLinkEvent;

    public void OnUpdate(ref SystemState state)
    {
        int count = 1;

        foreach (var (localTransform, ghostOwner, entity) in SystemAPI
            .Query<RefRO<LocalTransform>, RefRO<GhostOwner>>()
            .WithAll<CharacterComponent>()
            .WithNone<GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            OnMinimapTeamLinkEvent?.Invoke(this, new MinimapTeamArgs
            {
                TeamId = (int)PlayerHelpers.GetPlayerInTeam(ghostOwner.ValueRO.NetworkId),
                PlayerId = count,
                Position = localTransform.ValueRO.Position,
                Forward = localTransform.ValueRO.Forward(),
                Alive = state.EntityManager.IsComponentEnabled(entity, typeof(CharacterIsEnable))
            });

            count++;
        }
    }
}
