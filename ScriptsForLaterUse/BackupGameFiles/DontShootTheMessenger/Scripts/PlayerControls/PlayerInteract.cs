using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance = 3f;   // How far the player can reach
    public LayerMask npcLayer;            // Assign NPC layer in inspector

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame) // or use your input system
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        // Raycast in front of player to detect NPC
        Ray ray = new Ray(transform.position, transform.forward);
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
