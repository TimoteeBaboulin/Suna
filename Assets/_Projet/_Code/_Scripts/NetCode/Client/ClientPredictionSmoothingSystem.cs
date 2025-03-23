using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientPredictionSmoothingSystem : ISystem
{
    private static bool _enabled = true;
    private static bool _registered = false;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GhostPredictionSmoothing>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_registered || !_enabled) { return; }
        _registered = true;

        GhostPredictionSmoothing ghostPrediction = SystemAPI.GetSingleton<GhostPredictionSmoothing>();
        ghostPrediction.RegisterSmoothingAction<LocalTransform>(state.EntityManager, ClientTransformSmoothingAction.Action);
    }

}

[BurstCompile]
public unsafe struct ClientTransformSmoothingAction
{
    public sealed class DefaultStaticUserParams
    {
        internal static readonly SharedStatic<float> maxDist = SharedStatic<float>.GetOrCreate<DefaultStaticUserParams, MaxDistKey>();
        internal static readonly SharedStatic<float> delta = SharedStatic<float>.GetOrCreate<DefaultStaticUserParams, DeltaKey>();
        static DefaultStaticUserParams()
        {
            maxDist.Data = 0.1f; //For smoothing
            delta.Data = 0.01f; //For smoothing
        }
    }
    class MaxDistKey { }
    class DeltaKey { }

    public static readonly PortableFunctionPointer<GhostPredictionSmoothing.SmoothingActionDelegate> Action = new PortableFunctionPointer<GhostPredictionSmoothing.SmoothingActionDelegate>(SmoothingAction);

    [BurstCompile(DisableDirectCall = true)]
    private static void SmoothingAction(IntPtr currentData, IntPtr previousData, IntPtr usrData)
    {
        ref LocalTransform transform = ref UnsafeUtility.AsRef<LocalTransform>((void*)currentData);
        ref LocalTransform backup = ref UnsafeUtility.AsRef<LocalTransform>((void*)previousData);

        float maxDistance = DefaultStaticUserParams.maxDist.Data;
        float delta = DefaultStaticUserParams.delta.Data;

        if (usrData.ToPointer() != null)
        {
            ref DefaultSmoothingActionUserParams userPram = ref UnsafeUtility.AsRef<DefaultSmoothingActionUserParams>(usrData.ToPointer());
            maxDistance = userPram.maxDist;
            delta = userPram.delta;
        }

        float distance = math.distance(transform.Position, backup.Position);
        if (distance < maxDistance && distance > delta && distance > 0)
        {
            transform.Position = backup.Position + (transform.Position - backup.Position) * delta / distance;
        }
    }
}



