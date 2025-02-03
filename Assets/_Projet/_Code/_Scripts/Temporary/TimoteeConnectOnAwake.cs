using UnityEngine;

public class TimoteeConnectOnAwake : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() => ConnectionManager.Instance.Connect();

    private void OnEnable() => IRoundManager.OnRoundStart += (int a, int b) => { Debug.Log("Current score is: " + a + " to  " + b); };

    private void Update() => Debug.Log("Timer is " + IRoundManager.CurrentTime);
}
