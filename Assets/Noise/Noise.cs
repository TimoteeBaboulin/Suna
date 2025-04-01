public abstract class Noise
{
    abstract public float Noise2D(float x, float y);
    abstract public float Noise3D(float x, float y, float z);

    abstract public void SetParameters(params object[] parameters);
}
