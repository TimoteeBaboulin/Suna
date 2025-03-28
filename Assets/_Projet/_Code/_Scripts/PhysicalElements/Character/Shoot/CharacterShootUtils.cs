using Unity.Mathematics;
using UnityEngine;

public class CharacterShootUtils
{
    public static float2 TSprayPattern(in int bulletIndex, in float accuracy = 0f, in float amplifier = 10f, in float targetDistance = 0f)
    {
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
        //float randomTheta = UnityEngine.Random.Range(0, 2f) * Mathf.PI;
        //float radius = UnityEngine.Random.Range(0, accuracy * targetDistance * 3f / 100f);
        //currentRecoilXPos += Mathf.Cos(randomTheta) * radius;
        //currentRecoilYPos += Mathf.Sin(randomTheta) * radius;

        // Amplifying the effect
        currentRecoilYPos *= amplifier;
        currentRecoilXPos *= amplifier;

        return new(currentRecoilXPos, currentRecoilYPos);
    }
}
