using UnityEngine;

public class LookAction : PlayerAction
{
    public float sensitivity = 2f;
    private float pitch = 0f;

    public Transform cameraTransform;

    public bool cameraLocked = false;

    public void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    public void OnEnable()
    {
        // Lock cursor when looking around
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void LockCamera(bool locked)
    {
        cameraLocked = locked;
    }

    protected override void PerformOnServer(in ActionPayload payload)
    {
        // Optionally validate or clamp, then write to a NetworkVariable for rotation
        // If you want strict authority, compute final rotations here and replicate.
    }

    protected override void PerformOnClients(in ActionPayload payload)
    {
        if (cameraLocked) return;
        Vector2 look = corePlayer.LookInput * sensitivity;

        // Rotate player body
        transform.Rotate(Vector3.up * look.x);

        // Rotate camera (clamp vertical)
        pitch -= look.y;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
