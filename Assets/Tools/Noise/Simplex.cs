using System.Diagnostics;
using Unity.Burst;
using Unity.Mathematics;

public class Simplex : Noise
{
    private static readonly int[] PermutationTable = new int[512];
    private static readonly float HALF_OF_SQRT3_MINUS_ONE = 0.36603f;
    private static readonly float THREE_MINUS_SQRT3_OVER_SIX = 0.21132f;

    private float scale;

    public Simplex(float scale)
    {
        var permutation = new int[256];

        for (int i = 0; i < 256; i++)
        {
            permutation[i] = i;
        }

        for (int i = 255; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
        }

        for (int i = 0; i < 256; i++)
        {
            PermutationTable[i] = PermutationTable[i + 256] = permutation[i];
        }

        this.scale = scale;
    }

    [BurstCompile]
    private float2 Skew(float2 v) => v + math.csum(v) * HALF_OF_SQRT3_MINUS_ONE;
    [BurstCompile]
    private float3 Skew(float3 v) => v + math.csum(v) * 0.33333f;

    [BurstCompile]
    private float2 Unskew(float2 v) => v - math.csum(v) * THREE_MINUS_SQRT3_OVER_SIX;
    [BurstCompile]
    private float3 Unskew(float3 v) => v - math.csum(v) * 0.16667f;

    [BurstCompile]
    private float Grad(int hash, float x, float y)
    {
        int h = hash & 7;

        switch (h)
        {
            case 0: return x + y;
            case 1: return -x + y;
            case 2: return x - y;
            case 3: return -x - y;
            case 4: return y + x;
            case 5: return -y + x;
            case 6: return y - x;
            case 7: return -y - x;
            default: return 0;
        }
    }

    [BurstCompile]
    private float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;

        switch (h)
        {
            case 0: return x + y;
            case 1: return -x + y;
            case 2: return x - y;
            case 3: return -x - y;
            case 4: return x + z;
            case 5: return -x + z;
            case 6: return x - z;
            case 7: return -x - z;
            case 8: return y + z;
            case 9: return -y + z;
            case 10: return y - z;
            case 11: return -y - z;
            case 12: return y + x;
            case 13: return -y + z;
            case 14: return y - x;
            case 15: return -y - z;
            default: return 0;
        }
    }

    [BurstCompile]
    private float Contribution(float t, float grad)
    {
        if (t < 0) return 0f;
        t *= t;
        return t * t * grad;
    }

    [BurstCompile]
    public override float Noise2D(float x, float y)
    {
        float2 position = new float2(x, y) * scale;

        float2 s = Skew(position);
        int2 cell = (int2)math.floor(s);

        float2 unskewed = Unskew(cell);
        float2 offset = position - unskewed;

        int i1 = offset.x > offset.y ? 1 : 0;
        int j1 = offset.x > offset.y ? 0 : 1;

        float2 p1 = offset - new float2(i1, j1) + THREE_MINUS_SQRT3_OVER_SIX;
        float2 p2 = offset - 1f + 2f * THREE_MINUS_SQRT3_OVER_SIX;

        int gi0 = PermutationTable[cell.x & 255 + PermutationTable[cell.y & 255]] % 12;
        int gi1 = PermutationTable[(cell.x + i1) & 255 + PermutationTable[(cell.y + j1) & 255]] % 12;
        int gi2 = PermutationTable[(cell.x + 1) & 255 + PermutationTable[(cell.y + 1) & 255]] % 12;

        float t0 = 0.5f - math.dot(offset, offset);
        float t1 = 0.5f - math.dot(p1, p1);
        float t2 = 0.5f - math.dot(p2, p2);

        float n0 = t0 < 0 ? 0.0f : t0 * t0 * t0 * t0 * Grad(gi0, offset.x, offset.y);
        float n1 = t1 < 0 ? 0.0f : t1 * t1 * t1 * t1 * Grad(gi1, p1.x, p1.y);
        float n2 = t2 < 0 ? 0.0f : t2 * t2 * t2 * t2 * Grad(gi2, p2.x, p2.y);

        return ((70f * (n0 + n1 + n2)) + 1f) * 0.5f;
    }

    [BurstCompile]
    public override float Noise3D(float x, float y, float z)
    {
        float3 position = new float3(x, y, z) * scale;

        float3 s = Skew(position);
        int3 cell = (int3)math.floor(s);

        float3 unskewed = Unskew(cell);
        float3 offset = position - unskewed;

        int3 i1, i2;

        if (offset.x >= offset.y)
        {
            if (offset.y >= offset.z)
            {
                i1 = new int3(1, 0, 0);
                i2 = new int3(1, 1, 0);
            }
            else if (offset.x >= offset.z)
            {
                i1 = new int3(1, 0, 0);
                i2 = new int3(1, 0, 1);
            }
            else
            {
                i1 = new int3(0, 0, 1);
                i2 = new int3(1, 0, 1);
            }
        }
        else
        {
            if (offset.y < offset.z)
            {
                i1 = new int3(0, 0, 1);
                i2 = new int3(0, 1, 1);
            }
            else if (offset.x < offset.z)
            {
                i1 = new int3(0, 1, 0);
                i2 = new int3(0, 1, 1);
            }
            else
            {
                i1 = new int3(0, 1, 0);
                i2 = new int3(1, 1, 0);
            }
        }

        float3 p1 = offset - i1 + 0.16667f;
        float3 p2 = offset - i2 + 0.33333f;
        float3 p3 = offset - 1f + 0.5f;

        int gi0 = PermutationTable[cell.x & 255 + PermutationTable[cell.y & 255 + PermutationTable[cell.z & 255]]];
        int gi1 = PermutationTable[(cell.x + i1.x) & 255 + PermutationTable[(cell.y + i1.y) & 255 + PermutationTable[(cell.z + i1.z) & 255]]];
        int gi2 = PermutationTable[(cell.x + i2.x) & 255 + PermutationTable[(cell.y + i2.y) & 255 + PermutationTable[(cell.z + i2.z) & 255]]];
        int gi3 = PermutationTable[(cell.x + 1) & 255 + PermutationTable[(cell.y + 1) & 255 + PermutationTable[(cell.z + 1) & 255]]];

        float t0 = 0.6f - math.dot(offset, offset);
        float t1 = 0.6f - math.dot(p1, p1);
        float t2 = 0.6f - math.dot(p2, p2);
        float t3 = 0.6f - math.dot(p3, p3);

        float n0 = Contribution(t0, Grad(gi0, offset.x, offset.y, offset.z));
        float n1 = Contribution(t1, Grad(gi1, p1.x, p1.y, p1.z));
        float n2 = Contribution(t2, Grad(gi2, p2.x, p2.y, p2.z));
        float n3 = Contribution(t3, Grad(gi3, p3.x, p3.y, p3.z));

        return ((32f * (n0 + n1 + n2 + n3)) + 1f) * 0.5f;
    }

    public override void SetParameters(params object[] parameters)
    {
        this.scale = (float)parameters[0];
    }
}
