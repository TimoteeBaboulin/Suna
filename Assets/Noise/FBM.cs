using Unity.Burst;
using Unity.Mathematics;

public class FBM : Noise
{
    private Noise noise;
    private int octaves;
    private float lacunarity;
    private float persistence;

    public FBM(Noise noise, int octaves, float lacunarity, float persistence)
    {
        this.noise = noise;
        this.octaves = math.max(1, octaves);
        this.lacunarity = lacunarity;
        this.persistence = persistence;

        if(this.noise is FBM)
        {
            this.noise = new Perlin(0.1f, 0);
            throw new System.ArgumentException("Can't nest FBM noise");
        }
    }

    [BurstCompile]
    public override float Noise2D(float x, float y)
    {
        float value = 0;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            value += ((noise.Noise2D(x * frequency, y * frequency) * 2f) - 1f) * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return ((value + 1f) * 0.5f);
    }

    [BurstCompile]
    public override float Noise3D(float x, float y, float z)
    {
        float value = 0;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            value += ((noise.Noise3D(x * frequency, y * frequency, z * frequency) * 2f) - 1f) * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return ((value + 1f) * 0.5f);
    }

    public override void SetParameters(params object[] parameters)
    {
        this.noise = (Noise)parameters[0];
        this.octaves = (int)parameters[1];
        this.lacunarity = (float)parameters[2];
        this.persistence = (float)parameters[3];

        object[] objects = new object[parameters.Length - 4];
        System.Array.Copy(parameters, 4, objects, 0, parameters.Length - 4);

        this.noise.SetParameters(objects);
    }
}
