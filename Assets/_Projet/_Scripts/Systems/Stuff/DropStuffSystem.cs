using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct DropStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterStuffList>();
        state.RequireForUpdate<CharacterInput>();
        state.RequireForUpdate<UnequipStuffQueue>();
        state.RequireForUpdate<GameResourcesDatabase>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Get ECB
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var unequipStuffQueue = SystemAPI.GetSingletonBuffer<UnequipStuffQueue>();
        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();

        foreach (var (stuffList, linkedBuffer, stuffInfosRO, inputRO, transformRO, shootStartPosDeltaRO, viewRO, chara) in SystemAPI
        .Query<DynamicBuffer<CharacterStuffList>,
        DynamicBuffer<LinkedEntityGroup>,
        RefRO<CharacterStuffInfos>,
        RefRO<CharacterInput>,
        RefRO<LocalTransform>,
        RefRO<CharacterShootStartPositionDelta>,
        RefRO<CharacterViewRotation>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRO.ValueRO;
            ref readonly CharacterStuffInfos stuffInfos = ref stuffInfosRO.ValueRO;
            if (input.drop.IsSet && stuffInfos.StuffInHandSlot != StuffSlot.Melee)
            {
                Entity stuffInHand = StuffUtils.GetStuffInHand(stuffList, stuffInfos);
                var stuffGhostOwnerRW = SystemAPI.GetComponentRW<GhostOwner>(stuffInHand);
                ref var stuffCommonData = ref SystemAPI.GetComponentRO<StuffDatabaseAccess>(stuffInHand).ValueRO.GetData(ref database);
                StuffUtils.Drop(ref state, ref ecb, linkedBuffer, stuffList, stuffGhostOwnerRW, ref stuffCommonData, chara, stuffInHand, shootStartPosDeltaRO, viewRO, transformRO, 5f);

                //StuffUtils.UnequipNextFrame(unequipStuffQueue, chara, stuffInHand);
            }
        }
    }
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct StuffDropedCleanup : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Get ECB
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (stuffInHandRef, dropedStuff) in SystemAPI.Query<RefRO<StuffEntityInHandRef>>().WithEntityAccess())
        {
            if (stuffInHandRef.ValueRO.Value == Entity.Null)
            {
                UnityEngine.Debug.Log("StuffDropedCleanup Run");
                ecb.DestroyEntity(dropedStuff);
            }
            else if(state.EntityManager.HasComponent<StuffDynamicData>(stuffInHandRef.ValueRO.Value))
            {
                var stuffDynData = state.EntityManager.GetComponentData<StuffDynamicData>(stuffInHandRef.ValueRO.Value);
                if (stuffDynData.dropedEntityRef == Entity.Null)
                {
                    UnityEngine.Debug.Log("StuffDropedCleanup Run");
                    ecb.DestroyEntity(dropedStuff);
                }
            }
        }
    }
}