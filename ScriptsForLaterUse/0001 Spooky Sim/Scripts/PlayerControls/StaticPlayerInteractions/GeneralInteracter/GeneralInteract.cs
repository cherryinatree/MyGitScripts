using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class InteractGameObjectEvent : UnityEvent<GameObject> { }
/// <summary>
/// Generic event-driven interactable.
/// - Add this to any object with a Collider (and optional trigger for proximity).
/// - In the Inspector, wire functions to the events.
/// - When the player interacts, the events fire.
/// </summary>
public class GeneralInteract : Interactable
{
    [Header("Events")]
    [Tooltip("Invoked when Interact() is called (no parameters).")]
    public UnityEvent onInteract;

    [Tooltip("Invoked when Interact() is called, passing the interactor GameObject.")]
    public InteractGameObjectEvent onInteractWithActor;

    [Header("Optional Focus Feedback")]
    public UnityEvent onFocusGained;
    public UnityEvent onFocusLost;

    [Header("Rules")]
    [Tooltip("Seconds between allowed uses. 0 = no cooldown.")]
    [Min(0f)] public float cooldown = 0f;

    [Tooltip("Disable this component after a successful interaction.")]
    public bool disableAfterUse = false;

    [Tooltip("Deactivate the whole GameObject after a successful interaction.")]
    public bool deactivateAfterUse = false;

    private float _lastUseTime = -Mathf.Infinity;

    public override bool CanInteract(GameObject interactor)
    {
        // Keep base checks (enabled, activeInHierarchy), then apply cooldown.
        return base.CanInteract(interactor) && (Time.time >= _lastUseTime + cooldown);
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;

        // Fire both events (use whichever you wired).
        onInteract?.Invoke();
        onInteractWithActor?.Invoke(interactor);

        _lastUseTime = Time.time;

        if (disableAfterUse) enabled = false;
        if (deactivateAfterUse) gameObject.SetActive(false);
    }

    public override void OnFocusGained(GameObject interactor)
    {
        onFocusGained?.Invoke();
    }

    public override void OnFocusLost(GameObject interactor)
    {
        onFocusLost?.Invoke();
    }
}