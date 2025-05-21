using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public Vector3 startRotation = Vector3.zero;
    public float moveAcceleration = 25f;
    public float maxMoveSpeed = 10f;
    public float friction = 10f;
    public float mouseSensitivity = 100f;
    public bool controllerEnabled = false;
    public BoxCollider bounds;

    private float pitch = 0f;
    private float yaw = 0f;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        pitch = startRotation.x;
        yaw = startRotation.y;
    }

    void Update()
    {
        if (!controllerEnabled)
        {
            velocity = Vector3.zero;
            return;
        }   

        RotateCamera();
        MoveCamera();
        ClampPositionWithinBounds();
    }

    void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f); // empêche de faire un flip complet

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void MoveCamera()
    {
        Vector3 input = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) input += transform.forward;
        if (Input.GetKey(KeyCode.S)) input -= transform.forward;
        if (Input.GetKey(KeyCode.D)) input += transform.right;
        if (Input.GetKey(KeyCode.A)) input -= transform.right;
        if (Input.GetKey(KeyCode.Space)) input += transform.up;
        if (Input.GetKey(KeyCode.LeftControl)) input -= transform.up;

        input = input.normalized;

        if (input != Vector3.zero)
        {
            velocity += input * moveAcceleration * Time.deltaTime;
            velocity = Vector3.ClampMagnitude(velocity, maxMoveSpeed);
        }
        else
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, friction * Time.deltaTime);
        }

        transform.position += velocity * Time.deltaTime;
    }

    void ClampPositionWithinBounds()
    {
        if (!bounds) return;

        Bounds b = bounds.bounds;
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, b.min.x, b.max.x);
        pos.y = Mathf.Clamp(pos.y, b.min.y, b.max.y);
        pos.z = Mathf.Clamp(pos.z, b.min.z, b.max.z);

        transform.position = pos;
    }
}
