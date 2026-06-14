using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SprintAction : WalkAction
{
    [Header("Sprint Settings")]
    public float sprintMultiplier = 2f;       // How fast you move while sprinting
    public float maxStamina = 5f;             // Seconds of sprint available
    public float staminaDrainRate = 1f;       // Stamina drain per second
    public float staminaRegenRate = 2f;       // Stamina regen per second
    public float minStaminaToSprint = 0.1f;   // Minimum stamina needed to sprint

    private float currentStamina;

    protected void Awake()
    {
        currentStamina = maxStamina;
    }

    protected override void PerformOnServer(in ActionPayload payload)
    {
        // Optionally validate or clamp, then write to a NetworkVariable for rotation
        // If you want strict authority, compute final rotations here and replicate.
    }

    protected override void PerformOnClients(in ActionPayload payload)
    {
        //float speed = moveSpeed;
        bool canSprint = currentStamina > minStaminaToSprint;

        // Sprinting logic
        if (corePlayer.SprintInput && canSprint && corePlayer.MoveInput.magnitude > 0f)
        {
            //speed *= sprintMultiplier;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
        }
        else
        {
            // Only regenerate if player is NOT holding sprint
            if (!corePlayer.SprintInput)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }

        // Move player
        Vector3 move = new Vector3(corePlayer.MoveInput.x, 0, corePlayer.MoveInput.y);
        //controller.SimpleMove(transform.TransformDirection(move) * speed);
    }

    // Expose stamina for UI
    public float GetStaminaNormalized()
    {
        return currentStamina / maxStamina;
    }
}
