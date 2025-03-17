using UnityEngine;

public class TestShootRecoil : MonoBehaviour
{
    public TestCameraPivotRecoil patternAffectRotationScript;
    public GameObject impactPrefab;

    public int roundsPerMinutes = 1;

    public float timeTillNextFire;
    [Range(.01f, 2f)] public float timeSinceLastFireMax = .7f;
    public float timeSinceLastFire = 0f;
    public float currentRecoilXPos;
    public float currentRecoilYPos;

    public float accuracy = .5f;

    public int bulletIndex = 0;

    public float timePressed = 0f;

    private void Update()
    {
        Firing();
    }

    public void RecoilMath(float targetDistance = 0f)
    {
        Vector2 pattern = TPattern(bulletIndex, accuracy, targetDistance);

        patternAffectRotationScript.patternAffectedCameraXRotation -= pattern.y;
        patternAffectRotationScript.patternAffectedYRotation -= pattern.x;
    }

    public Vector2 TPattern(int bulletIndex, float accuracy = 0f, float targetDistance = 0f)
    {
        float currentRecoilXPos = 0f;
        float currentRecoilYPos = 0f;

        float amplifier = 10f;
        int bulletCutPattern = 17;
        if (bulletIndex < bulletCutPattern)
        {
            currentRecoilXPos = 0;
            currentRecoilYPos = 10 / (float)bulletCutPattern;
        }
        else
        {
            currentRecoilXPos = 2.5f * Mathf.Cos(bulletIndex - bulletCutPattern);
            currentRecoilYPos = 10 / (float)bulletCutPattern;
        }

        currentRecoilXPos *= 4f;
        currentRecoilYPos *= 4f;

        float randomTheta = Random.Range(0, 2f) * Mathf.PI;
        float radius = Random.Range(0, accuracy * targetDistance * 3f / 100f);
        currentRecoilXPos += Mathf.Cos(randomTheta) * radius;
        currentRecoilYPos += Mathf.Sin(randomTheta) * radius;

        currentRecoilYPos *= amplifier * Time.deltaTime;
        currentRecoilXPos *= amplifier * Time.deltaTime;

        return new(currentRecoilXPos, currentRecoilYPos);
    }
    public Vector2 InfinityPattern(int bulletIndex, float accuracy = 0f, float targetDistance = 0f)
    {
        float currentRecoilXPos = 0f;
        float currentRecoilYPos = 0f;

        float amplifier = 10f;
        currentRecoilXPos = 8f * Mathf.Cos(2 * bulletIndex);
        currentRecoilYPos = 8f * Mathf.Sin(bulletIndex);

        currentRecoilXPos *= 4f;
        currentRecoilYPos *= 4f;

        float randomTheta = Random.Range(0, 2f) * Mathf.PI;
        float radius = Random.Range(0, accuracy * targetDistance * 3f / 100f);
        currentRecoilXPos += Mathf.Cos(randomTheta) * radius;
        currentRecoilYPos += Mathf.Sin(randomTheta) * radius;

        currentRecoilYPos *= amplifier * Time.deltaTime;
        currentRecoilXPos *= amplifier * Time.deltaTime;

        return new(currentRecoilXPos, currentRecoilYPos);
    }
    public Vector2 CirclePattern(int bulletIndex, float accuracy = 0f, float targetDistance = 0f)
    {
        float currentRecoilXPos = 0f;
        float currentRecoilYPos = 0f;

        float amplifier = 10f;
        currentRecoilXPos = 8f * Mathf.Cos(bulletIndex);
        currentRecoilYPos = 8f * Mathf.Sin(bulletIndex);

        currentRecoilXPos *= 4f;
        currentRecoilYPos *= 4f;

        float randomTheta = Random.Range(0, 2f) * Mathf.PI;
        float radius = Random.Range(0, accuracy * targetDistance * 3f / 100f);
        currentRecoilXPos += Mathf.Cos(randomTheta) * radius;
        currentRecoilYPos += Mathf.Sin(randomTheta) * radius;

        currentRecoilYPos *= amplifier * Time.deltaTime;
        currentRecoilXPos *= amplifier * Time.deltaTime;

        return new(currentRecoilXPos, currentRecoilYPos);
    }

    private void Firing()
    {
        if (Input.GetMouseButton(0))
        {
            Fire();
            timePressed += Time.deltaTime;
            patternAffectRotationScript.isFiring = true;
        }
        else
        {
            currentRecoilXPos = 0f;
            currentRecoilYPos = 0f;
            timePressed = 0f;
            patternAffectRotationScript.isFiring = false;
        }
        timeSinceLastFire += Time.deltaTime;
        timeTillNextFire -= roundsPerMinutes / 60f * Time.deltaTime;

        if (timeTillNextFire < 0)
        {
            timeTillNextFire = -1f;
        }

        if (timeSinceLastFire > timeSinceLastFireMax)
        {
            bulletIndex = 0;
        }
    }

    private void Fire()
    {
        if (timeTillNextFire <= 0)
        {

            timeTillNextFire = 1;

            timeSinceLastFire = 0f;

            bulletIndex++;

            if (Physics.Raycast(transform.position, patternAffectRotationScript.camTransform.forward, out RaycastHit hit, 100f))
            {
                Instantiate(impactPrefab, hit.point, Quaternion.identity);
                RecoilMath(hit.distance);
            }
            else
            {

                RecoilMath();
            }
        }
    }
}
