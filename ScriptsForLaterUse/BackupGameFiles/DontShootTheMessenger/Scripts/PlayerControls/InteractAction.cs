using UnityEngine;

public class InteractAction : PlayerAction
{
    public float interactRange = 3f;


    protected override void PerformOnServer(in ActionPayload payload)
    {
        // Optionally validate or clamp, then write to a NetworkVariable for rotation
        // If you want strict authority, compute final rotations here and replicate.
    }

    protected override void PerformOnClients(in ActionPayload payload)
    {
        if (corePlayer.InteractInput)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
            {
                if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
                {
                    interactable.OnInteract();
                }
            }
        }
    }
}
