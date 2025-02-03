using UnityEngine;

public class ConnectionDebug : MonoBehaviour
{
    [SerializeField] private ConnectionManager connectionManagerPrefab;
    private ConnectionManager connectionInstance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        connectionInstance = FindAnyObjectByType<ConnectionManager>();
        if (connectionInstance == null)
        {
            connectionInstance = Instantiate(connectionManagerPrefab);
            connectionInstance.IP = "141.94.194.103";
        }
        else
        {
            Destroy(connectionInstance.gameObject);
        }
    }

    public void Connect()
    {
        connectionInstance.Connect();
    }
}
