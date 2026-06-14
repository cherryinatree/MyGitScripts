using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class JumpAction : PlayerAction
{
    [Header("Jump Settings")]
    public float jumpHeight = 2f;       // Maximum jump height
    public float gravity = -9.81f;      // Gravity force
    public float coyoteTime = 0.2f;     // Extra time after leaving ground to still jump

    private CharacterController controller;
    private float verticalVelocity;
    private float coyoteTimer;

    protected void Awake()
    {

        controller = GetComponent<CharacterController>();
    }

    protected override void PerformOnServer(in ActionPayload payload)
    {
        // Optionally validate or clamp, then write to a NetworkVariable for rotation
        // If you want strict authority, compute final rotations here and replicate.
    }

    protected override void PerformOnClients(in ActionPayload payload)
    {
        bool isGrounded = controller.isGrounded;

        // Reset coyote timer when grounded
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (verticalVelocity < 0f)
                verticalVelocity = -1f; // Small negative to keep grounded smoothly
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        // Jump (triggered once per key press)
        if (corePlayer != null && corePlayer.JumpPressedThisFrame && coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteTimer = 0f; // consume coyote time
        }

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        // Move player vertically
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
