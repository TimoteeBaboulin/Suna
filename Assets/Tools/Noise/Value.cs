using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

public enum InterpolationType
{
    Linear,
    Cosine,
    Cubic
}

public class Value : Noise
{
    private uint verticeCount;
    private uint verticeCountPerAxis;
    private float[] values;
    private InterpolationType interpolationType;

    public Value(uint verticeCount, InterpolationType interpolationType)
    {
        this.verticeCount = verticeCount;
        this.verticeCountPerAxis = verticeCount; // By default it's a one-dimensional noise
        values = new float[verticeCount];

        for (int i = 0; i < verticeCount; i++)
        {
            values[i] = UnityEngine.Random.Range(0f, 1f);
        }

        this.interpolationType = interpolationType;
    }

    [BurstCompile]
    public float Interpolate(float a, float b, float t)
    {
        switch (interpolationType)
        {
            case InterpolationType.Linear:
                return math.lerp(a, b, t);
            case InterpolationType.Cosine:
                return math.lerp(a, b, 0.5f - math.cos(t * math.PI) * 0.5f);
            case InterpolationType.Cubic:
                return math.lerp(a, b, t * t * (3 - 2 * t));
            default:
                return 0;
        }
    }

    [BurstCompile]
    public override float Noise2D(float x, float y)
    {
        float2 position = new float2(x, y);
        float2 chunkPosition = position / verticeCountPerAxis;

        float topLeft = values[((int)chunkPosition.x + (int)chunkPosition.y * verticeCountPerAxis) % verticeCount];
        float topRight = values[((int)chunkPosition.x + 1 + (int)chunkPosition.y * verticeCountPerAxis) % verticeCount];
        float bottomLeft = values[((int)chunkPosition.x + (int)chunkPosition.y * verticeCountPerAxis + verticeCountPerAxis) % verticeCount];
        float bottomRight = values[((int)chunkPosition.x + 1 + (int)chunkPosition.y * verticeCountPerAxis + verticeCountPerAxis) % verticeCount];

        float2 lerps = math.frac(chunkPosition);

        float top = Interpolate(topLeft, topRight, lerps.x);
        float bottom = Interpolate(bottomLeft, bottomRight, lerps.x);

        return Interpolate(top, bottom, lerps.y);
    }

    [BurstCompile]
    public override float Noise3D(float x, float y, float z)
    {
        float3 position = new float3(x, y, z);
        float3 chunkPosition = position / verticeCountPerAxis;

        float closeTopLeft = values[((int)chunkPosition.x + (int)chunkPosition.y * verticeCountPerAxis + (int)chunkPosition.z * verticeCountPerAxis * verticeCountPerAxis) % verticeCount];
        float closeTopRight = values[((int)chunkPosition.x + 1 + (int)chunkPosition.y * verticeCountPerAxis + (int)chunkPosition.z * verticeCountPerAxis * verticeCountPerAxis) % verticeCount];
        float closeBottomLeft = values[((int)chunkPosition.x + (int)chunkPosition.y * verticeCountPerAxis + verticeCountPerAxis + (int)chunkPosition.z * verticeCountPerAxis * verticeCountPerAxis) % verticeCount];
        float closeBottomRight = values[((int)chunkPosition.x + 1 + (int)chunkPosition.y * verticeCountPerAxis + verticeCountPerAxis + (int)chunkPosition.z * verticeCountPerAxis * verticeCountPerAxis) % verticeCount];

        float farTopLeft = values[((int)chunkPosition.x + (int)chunkPosition.y * verticeCountPerAxis + (int)chunkPosition.z * verticeCountPerAxis * verticeCountPerAxis + verticeCountPerAxis) % verticeCount];
        float farTopRight = values[((int)chunkPosition.x + 1 + (int)chunkPosition.y * verticeCountPerAxis + (int)chunkPosition.z * verticeCountPerAxis * verticeCountPerAxis + verticeCountPerAxis) % verticeCount];
        float farBottomLeft = values[((int)chunkPosition.x + (int)chunkPosition.y * verticeCountPerAxis + verticeCountPerAxis + (int)chunkPosition.z * verticeCountPerAxis * verticeCountPerAxis + verticeCountPerAxis) % verticeCount];
        float farBottomRight = values[((int)chunkPosition.x + 1 + (int)chunkPosition.y * verticeCountPerAxis + verticeCountPerAxis + (int)chunkPosition.z * verticeCountPerAxis * verticeCountPerAxis + verticeCountPerAxis) % verticeCount];

        float3 lerps = math.frac(chunkPosition);

        float closeTop = Interpolate(closeTopLeft, closeTopRight, lerps.x);
        float closeBottom = Interpolate(closeBottomLeft, closeBottomRight, lerps.x);

        float farTop = Interpolate(farTopLeft, farTopRight, lerps.x);
        float farBottom = Interpolate(farBottomLeft, farBottomRight, lerps.x);

        float close = Interpolate(closeTop, closeBottom, lerps.y);
        float far = Interpolate(farTop, farBottom, lerps.y);

        return Interpolate(close, far, lerps.z);
    }

    public override void SetParameters(params object[] parameters)
    {
        if ((int)parameters[2] == 0) //2D
        {
            verticeCount = (uint)parameters[0];
            verticeCount *= verticeCount; //we have vc^2 vertices

            verticeCountPerAxis = (uint)parameters[0];
        }
        else if ((int)parameters[2] == 1) //3D
        {
            verticeCount = (uint)parameters[0];
            verticeCount *= verticeCount * verticeCount; //we have vc^3 vertices

            verticeCountPerAxis = (uint)parameters[0];
        }

        values = new float[verticeCount];

        for (int i = 0; i < verticeCount; i++)
        {
            values[i] = UnityEngine.Random.Range(0f, 1f);
        }

        interpolationType = (InterpolationType)parameters[1];
    }
}
