using UnityEngine;

public class TimoteeConnectOnAwake : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ConnectionManager.Instance.Connect();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
