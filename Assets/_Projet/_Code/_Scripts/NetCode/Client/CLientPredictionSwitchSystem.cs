using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Services.Matchmaker.Models;
using Unity.Transforms;
using UnityEngine;


[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class CLientPredictionSwitchSystem : SystemBase
{
    private float predictionRadius = 10.0f;
    private float predictionMargin = 5.0f;

    private NativeList<Entity> entitiesToPredicted;
    private NativeList<Entity> entitiesToInterporlated;

    protected override void OnCreate()
    {
        RequireForUpdate<BombData>();
        entitiesToPredicted = new NativeList<Entity>(initialCapacity: 10, Allocator.Persistent); //Here replace 10 by NbOfPlayers in session once created
        entitiesToInterporlated = new NativeList<Entity>(initialCapacity: 10, Allocator.Persistent); //Here replace 10 by NbOfPlayers in session once created
    }

    protected override void OnDestroy()
    {
        entitiesToPredicted.Dispose();
        entitiesToInterporlated.Dispose();
    }

    protected override void OnUpdate()
    {
        if (SystemAPI.TryGetSingleton<GhostPredictionSwitchingQueues>(out var ghostSwitchingQueue))
        {
            ghostSwitchingQueue = SystemAPI.GetSingletonRW<GhostPredictionSwitchingQueues>().ValueRW;
            var toPredicted = entitiesToPredicted;
            var toInterpotalted = entitiesToInterporlated;

            for (int i = 0; i < toPredicted.Length; i++)
            {
                if (EntityManager.HasComponent<GhostInstance>(toPredicted[i]))
                {
                    ghostSwitchingQueue.ConvertToPredictedQueue.Enqueue(new ConvertPredictionEntry
                    {
                        TargetEntity = toPredicted[i],
                        TransitionDurationSeconds = 0.0f
                    });
                }
            }

            for (int i = 0; i < toInterpotalted.Length; i++)
            {
                if (EntityManager.HasComponent<GhostInstance>(toInterpotalted[i]))
                {
                    ghostSwitchingQueue.ConvertToInterpolatedQueue.Enqueue(new ConvertPredictionEntry
                    {
                        TargetEntity = toInterpotalted[i],
                        TransitionDurationSeconds = 0.0f
                    });
                }
            }


            toPredicted.Clear();
            toInterpotalted.Clear();

            if (!SystemAPI.TryGetSingletonEntity<CharacterComponent>(out var playerEntity) || !EntityManager.HasComponent<LocalTransform>(playerEntity))
            {
                return;
            }

            float3 playerPosition = EntityManager.GetComponentData<LocalTransform>(playerEntity).Position;
            float radius = predictionRadius;
            float margin = predictionMargin;
            Entities.WithNone<PredictedGhost>().WithAll<BombData>().ForEach((Entity bomb, in LocalTransform bombTransform) =>
            {
                if (math.distance(playerPosition, bombTransform.Position) < radius)
                {
                    toPredicted.Add(bomb);
                    Debug.Log("toPredicted");
                }
            }).Schedule();

            float radiusInterpolated = radius + margin;

            Entities.WithAll<PredictedGhost>().WithAll<BombData>().ForEach((Entity bomb, in LocalTransform bombTransform) =>
            {
                if (math.distance(playerPosition, bombTransform.Position) > radiusInterpolated)
                {
                    toInterpotalted.Add(bomb);
                    Debug.Log("toInterpotalted");
                }
            }).Schedule();


            //Ici example de passage de proprietaire
            Entities.WithAll<BombData>().ForEach((Entity bomb, ref BombData bombData, in LocalTransform bombTransform) =>
            {
                if (math.distance(playerPosition, bombTransform.Position) < radius) //Actuellement changement fait via la position dans un rayon
                {
                    if (bombData.owner == Entity.Null || bombData.owner != playerEntity)
                    {
                        bombData.owner = playerEntity;
                        Debug.Log($"Bomb ownership changed to Player: {playerEntity.Index}");
                    }
                }
            }).Schedule();
        }
    }
}
