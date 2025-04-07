using Unity.Burst;
using UnityEngine;

public class White : Noise
{
    public White()
    {

    }

    [BurstCompile]
    public override float Noise2D(float x, float y)
    {
        return Random.value;
    }

    [BurstCompile]
    public override float Noise3D(float x, float y, float z)
    {
        return Random.value;
    }

    public override void SetParameters(params object[] parameters)
    {
        // No parameters to modify
    }
}
