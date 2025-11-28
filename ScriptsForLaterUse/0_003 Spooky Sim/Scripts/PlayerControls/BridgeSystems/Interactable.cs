using UnityEngine;

/// <summary>
/// Base class for interactable objects. Add a Collider:
/// - For proximity, use an extra trigger collider (isTrigger = true).
/// - For raycast targeting, any collider works.
/// Derive and override Interact/CanInteract/Focus methods.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    [Header("Interactable")]
    [Tooltip("Optional display name/prompt for UI.")]
    public string displayName = "Interact";

    [Tooltip("Optional override focus point; defaults to transform.position.")]
    public Transform focusOverride;

    /// <summary>Where the player should consider the object to be (for distance/aim).</summary>
    public Vector3 FocusPoint => focusOverride ? focusOverride.position : transform.position;

    /// <summary>Can the given interactor use this right now?</summary>
    public virtual bool CanInteract(GameObject interactor) => enabled && gameObject.activeInHierarchy;

    /// <summary>Do the thing.</summary>
    public abstract void Interact(GameObject interactor);

    /// <summary>Optional: highlight, show prompt, etc.</summary>
    public virtual void OnFocusGained(GameObject interactor) { /* e.g., enable outline */ }

    /// <summary>Optional: unhighlight.</summary>
    public virtual void OnFocusLost(GameObject interactor) { /* e.g., disable outline */ }

    // ---- Proximity registration (requires a TRIGGER collider on this object or a child) ----
    private void OnTriggerEnter(Collider other)
    {
        var action = other.GetComponentInParent<InteractAction>();
        if (action != null) action.RegisterProximity(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var action = other.GetComponentInParent<InteractAction>();
        if (action != null) action.UnregisterProximity(this);
    }
}
