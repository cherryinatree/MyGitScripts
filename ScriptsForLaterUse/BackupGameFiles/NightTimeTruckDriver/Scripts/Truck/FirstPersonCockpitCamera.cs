using UnityEngine;
using UnityEngine.InputSystem; // If using new input system (optional)

public class FirstPersonCockpitCamera : MonoBehaviour
{
    public Transform playerBody; // This rotates left/right (truck base)
    public float mouseSensitivity = 100f;
    public float minPitch = -40f;
    public float maxPitch = 60f;

    private float pitch = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Mouse input
        float mouseX = Mouse.current.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime;
        float mouseY = Mouse.current.delta.y.ReadValue() * mouseSensitivity * Time.deltaTime;

        // Vertical rotation (pitch)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Horizontal rotation (yaw)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}