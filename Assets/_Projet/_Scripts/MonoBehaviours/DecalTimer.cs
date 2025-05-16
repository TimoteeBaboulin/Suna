using UnityEngine;

public class DecalTimer : MonoBehaviour
{
    public float _timer;

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            Destroy(gameObject);
        }
    }
}
