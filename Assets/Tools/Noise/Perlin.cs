using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

public class Perlin : Noise
{
    private static int[] PermutationTable;

    private static readonly float2[] Gradients2D =
    {
        new float2(1, 0), new float2(-1, 0), new float2(0, 1), new float2(0, -1),
        new float2(0.707f, 0.707f), new float2(-0.707f, 0.707f), new float2(0.707f, -0.707f), new float2(-0.707f, -0.707f),
    };

    private float frequency;
    private int period;

    public Perlin(float frequency, int period)
    {
        this.frequency = frequency;
        this.period = period;

        SetupPermutationTable();
    }

    [BurstCompile]
    private void SetupPermutationTable()
    {
        var permutation = new int[period];
        PermutationTable = new int[period * 2];

        for (int i = 0; i < period; i++)
        {
            permutation[i] = i;
        }

        for (int i = period - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
        }

        for (int i = 0; i < period; i++)
        {
            PermutationTable[i] = PermutationTable[i + period] = permutation[i];
        }
    }

    [BurstCompile]
    private float2 Fade(float2 t) => t * t * t * (t * (t * 6f - 15f) + 10f);
    [BurstCompile]
    private float3 Fade(float3 t) => t * t * t * (t * (t * 6f - 15f) + 10f); // Duplicate but needed for 3D noise

    [BurstCompile]
    private int Permute(int i)
    {
        return PermutationTable[i % period];
    }

    [BurstCompile]
    private float2 Grad(int x, int y)
    {
        int h = Permute(Permute(x) + y) ;
        return Gradients2D[h % 8];
    }

    [BurstCompile]
    private float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;

        switch(h)
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
    public override float Noise2D(float x, float y)
    {
        x = (x % period) * frequency;
        y = (y % period) * frequency;

        int X = (int)math.floor(x) % period;
        int Y = (int)math.floor(y) % period;

        float2 frac = math.frac(new float2(x, y));
        float2 u = Fade(frac);

        float2 d00 = frac - new float2(0, 0);
        float2 d01 = frac - new float2(0, 1);
        float2 d10 = frac - new float2(1, 0);
        float2 d11 = frac - new float2(1, 1);

        float2 grad00 = Grad(X, Y);
        float2 grad01 = Grad(X, Y + 1);
        float2 grad10 = Grad(X + 1, Y);
        float2 grad11 = Grad(X + 1, Y + 1);

        float n00 = math.dot(grad00, d00);
        float n01 = math.dot(grad01, d01);
        float n10 = math.dot(grad10, d10);
        float n11 = math.dot(grad11, d11);

        float lerpX1 = math.lerp(n00, n10, u.x);
        float lerpX2 = math.lerp(n01, n11, u.x);
        return (math.lerp(lerpX1, lerpX2, u.y) + 1.0f) * 0.5f;
    }

    [BurstCompile]
    public override float Noise3D(float x, float y, float z)
    {
        return 0.5f;

        /*if(loop > 0.0f)
        {
            x = math.fmod(x, loop);
            y = math.fmod(y, loop);
            z = math.fmod(z, loop);
        }

        x *= scale;
        y *= scale;
        z *= scale;

        int X = (int)math.floor(x) & 255;
        int Y = (int)math.floor(y) & 255;
        int Z = (int)math.floor(z) & 255;

        float3 frac = math.frac(new float3(x, y, z));
        float3 u = Fade(frac);

        int A = PermutationTable[X    ] + Y;
        int B = PermutationTable[X + 1] + Y;

        int AA = PermutationTable[A    ] + Z;
        int AB = PermutationTable[A + 1] + Z;
        int BA = PermutationTable[B    ] + Z;
        int BB = PermutationTable[B + 1] + Z;

        int AAA = PermutationTable[AA    ];
        int AAB = PermutationTable[AA + 1];
        int ABA = PermutationTable[AB    ];
        int ABB = PermutationTable[AB + 1];
        int BAA = PermutationTable[BA    ];
        int BAB = PermutationTable[BA + 1];
        int BBA = PermutationTable[BB    ];
        int BBB = PermutationTable[BB + 1];

        float gradAAA = Grad(AAA, frac.x, frac.y, frac.z);
        float gradAAB = Grad(AAB, frac.x, frac.y, frac.z - 1f);
        float gradABA = Grad(ABA, frac.x, frac.y - 1f, frac.z);
        float gradABB = Grad(ABB, frac.x, frac.y - 1f, frac.z - 1f);
        float gradBAA = Grad(BAA, frac.x - 1f, frac.y, frac.z);
        float gradBAB = Grad(BAB, frac.x - 1f, frac.y, frac.z - 1f);
        float gradBBA = Grad(BBA, frac.x - 1f, frac.y - 1f, frac.z);
        float gradBBB = Grad(BBB, frac.x - 1f, frac.y - 1f, frac.z - 1f);

        float x1 = math.lerp(gradAAA, gradBAA, u.x);
        float x2 = math.lerp(gradABA, gradBBA, u.x);
        float y1 = math.lerp(x1, x2, u.y);
        x1 = math.lerp(gradAAB, gradBAB, u.x);
        x2 = math.lerp(gradABB, gradBBB, u.x);
        float y2 = math.lerp(x1, x2, u.y);

        return (math.lerp(y1, y2, u.z) + 1.0f) * 0.5f;*/
    }

    public override void SetParameters(params object[] parameters)
    {
        frequency = (float)parameters[0];
        period = (int)parameters[1];
        SetupPermutationTable();
    }
}

