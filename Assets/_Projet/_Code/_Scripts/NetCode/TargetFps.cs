using UnityEngine;

public class TargetFps : MonoBehaviour
{
    void Start()
    {
        Application.targetFrameRate = 60;
    }
}
