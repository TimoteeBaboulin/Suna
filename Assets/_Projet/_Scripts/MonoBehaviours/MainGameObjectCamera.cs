using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MainGameObjectCamera : MonoBehaviour
{
    public static Camera Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = GetComponent<Camera>();
    }

    private void OnDestroy()
    {
        if (Instance == GetComponent<Camera>())
        {
            Instance = null;
        }
    }
}
