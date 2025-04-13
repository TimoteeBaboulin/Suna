using UnityEngine;

public class TestPlayerViewRotation : MonoBehaviour
{
    public Transform theCamera;

    public float mouseSensitivity = 4f;

    private float rotationXVelocity = 0f;
    private float rotationYVelocity = 0f;

    public float yRotationSpeed = 0f;
    public float xCameraSpeed = 0;

    [HideInInspector] public float currentYRotation = 0f;
    [HideInInspector] public float wantedYRotation = 0f;

    [HideInInspector] public float currentCameraXRotation = 0f;
    [HideInInspector] public float wantedCameraXRotation = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        MouseControl();
    }

    private void MouseControl()
    {
        wantedYRotation += Input.GetAxis("Mouse X") * mouseSensitivity;
        wantedCameraXRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        wantedCameraXRotation = Mathf.Clamp(wantedCameraXRotation, -90f, 90f);

        currentYRotation = Mathf.SmoothDamp(currentYRotation, wantedYRotation, ref rotationYVelocity, yRotationSpeed);
        currentCameraXRotation = Mathf.SmoothDamp(currentCameraXRotation, wantedCameraXRotation, ref rotationXVelocity, xCameraSpeed);

        transform.rotation = Quaternion.Euler(0, currentYRotation, 0);
        theCamera.localRotation = Quaternion.Euler(currentCameraXRotation, 0, 0);
    }
}
