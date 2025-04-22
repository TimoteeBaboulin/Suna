using UnityEngine;

public class TestImpactSphere : MonoBehaviour
{
    public float secondsBeforeDeath = 5f;

    private void Awake()
    {
        Destroy(gameObject, secondsBeforeDeath);
    }
}
