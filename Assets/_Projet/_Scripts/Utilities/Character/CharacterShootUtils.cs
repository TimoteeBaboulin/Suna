using Unity.Mathematics;
using UnityEngine;

public class CharacterShootUtils
{
    public static float2 SKAR18Pattern(in int bulletIndex, in float accuracy = 0f, in float amplifier = 1f, in float targetDistance = 0f)
    {
        if(bulletIndex == 0) return new float2(0.0f, 0.0f);


        float y = 0;
        if (bulletIndex < 10)
            y = 0.25f * bulletIndex;
        else if (bulletIndex < 20)
            y = 2.5f - (bulletIndex - 10) * 0.05f;
        else
            y = 2f - 0.1f * (bulletIndex - 20);

        float x = 0;
        if (bulletIndex < 10)
            x = -math.log2(bulletIndex) / 22.5f;
        else if (bulletIndex < 18)
            x = -0.2f * (bulletIndex - 9);
        else if (bulletIndex < 26)
            x = -0.2f * (17 - 9) + 0.3f * (bulletIndex - 17);
        else
            x = math.sin(bulletIndex) * 0.1f;

        // Amplifying the effect
        x *= 180;
        y *= 110;

        float randomTheta = UnityEngine.Random.Range(0, 2f) * Mathf.PI;
        float radius = UnityEngine.Random.Range(0, accuracy * targetDistance * 3f / 100f);
        x += Mathf.Cos(randomTheta) * radius;
        y += Mathf.Sin(randomTheta) * radius;

        y *= amplifier;
        x *= amplifier;

        return new(x, -y);
    }

    public static float2 NelaraPattern(in int bulletIndex, in float accuracy = 0f, in float amplifier = 1f, in float targetDistance = 0f)
    {
        float y = 0;
        if (bulletIndex < 3)
            y = math.log2(bulletIndex + 1) * 0.2f;
        else
            y = 0.3f + (bulletIndex - 2) * 0.4f;

        float x = math.sin(4 + (bulletIndex % 8 - 4) / 8f);

        // Amplifying the effect
        x *= 100;
        y *= 50;

        float randomTheta = UnityEngine.Random.Range(0, 2f) * Mathf.PI;
        float radius = UnityEngine.Random.Range(0, accuracy * targetDistance * 3f / 100f);
        x += Mathf.Cos(randomTheta) * radius;
        y += Mathf.Sin(randomTheta) * radius;

        y *= amplifier;
        x *= amplifier;

        return new(x, -y);
    }

    public static float2 TSprayPattern(in int bulletIndex, in float accuracy = 0f, in float amplifier = 10f, in float targetDistance = 0f)
    {
        if (bulletIndex == 0) return new float2(0.0f, 0.0f);

        // Values to returned
        float currentRecoilXPos = 0f;
        float currentRecoilYPos = 0f;

        // Pattern differentiater
        int bulletCutPattern = 14;
        if (bulletIndex < bulletCutPattern / 2)
        {
            currentRecoilXPos = 0;
            currentRecoilYPos = 3f / 4f * (1 - bulletIndex % bulletCutPattern);
        }
        else
        {
            currentRecoilXPos = 2.5f * Mathf.Cos(bulletIndex - bulletCutPattern);
            currentRecoilYPos = 3f / 4f * (1 - bulletCutPattern / 2);
        }

        // Amplifying the effect
        currentRecoilXPos *= 8f;
        currentRecoilYPos *= 8f;

        // Random on the impact
        float randomTheta = UnityEngine.Random.Range(0, 2f) * Mathf.PI;
        float radius = UnityEngine.Random.Range(0, accuracy * targetDistance * 3f / 100f);
        currentRecoilXPos += Mathf.Cos(randomTheta) * radius;
        currentRecoilYPos += Mathf.Sin(randomTheta) * radius;

        // Amplifying the effect
        currentRecoilYPos *= amplifier;
        currentRecoilXPos *= amplifier;

        return new(currentRecoilXPos, currentRecoilYPos);
    }
}
