using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerInteractAction : PlayerAction
{
    public float interactDistance = 3f;   // How far the player can reach
    public LayerMask npcLayer;            // Assign NPC layer in inspector

    protected override void PerformOnServer(in ActionPayload payload)
    {
        // Optionally validate or clamp, then write to a NetworkVariable for rotation
        // If you want strict authority, compute final rotations here and replicate.
    }

    protected override void PerformOnClients(in ActionPayload payload)
    {
        
        if (corePlayer.InteractPressedThisFrame)
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 1f, transform.forward); // adjust ray height
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, npcLayer))
        {
            CoreNPC npc = hit.collider.GetComponent<CoreNPC>();
            if (npc != null)
            {
                npc.Interact();
            }
        }
    }
}


