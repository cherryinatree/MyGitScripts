using UnityEngine;

[DisallowMultipleComponent]
public class MessTrash : MessItem
{
    public int TrashAmount = 1;

    private void Reset() => Kind = MessKind.Trash;

    public override void Interact(GameObject interactor)
    {
        if (IsResolved) return;
        if (interactor == null) return;

        var carrier = interactor.GetComponentInChildren<TrashCarrier>() ?? interactor.GetComponent<TrashCarrier>();
        if (carrier == null) return;

        // We only support 1 object per pickup here
        if (!carrier.Pickup(this)) return;
    }

    // Called by TrashCarrier.Pickup
    public void MarkTaken()
    {
        IsResolved = true;

        // Remove from registry immediately so janitors stop targeting it
        MessRegistry.Unregister(this);

        // Disable the "mess behavior" but keep the object visible
        enabled = false;

        // Optional: tag/layer so player can't interact with carried trash
        // gameObject.layer = LayerMask.NameToLayer("IgnoreRaycast");
    }
}
