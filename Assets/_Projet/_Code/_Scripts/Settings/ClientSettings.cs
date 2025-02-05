using UnityEngine;

public class ClientSettings : MonoBehaviour
{
    public static ClientSettings Instance;

    public float Sensivity = 5;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
