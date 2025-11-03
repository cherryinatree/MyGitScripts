using UnityEngine;
using System; // for Vector2

public abstract class PlayerAction : MonoBehaviour
{
    protected CorePlayer core;

    [Tooltip("If empty, action works in any context. Otherwise, only when CorePlayer.CurrentContext matches one of these.")]
    [SerializeField] private string[] allowedContexts;

    protected virtual void Awake()
    {
        core = GetComponentInParent<CorePlayer>();
        if (core == null)
            Debug.LogError($"{name}: PlayerAction requires CorePlayer on self or parent.");
    }

    protected virtual void OnEnable()
    {
        if (core == null) return;
        Subscribe(core);
        core.OnContextChanged += HandleContextChanged;
    }

    protected virtual void OnDisable()
    {
        if (core == null) return;
        Unsubscribe(core);
        core.OnContextChanged -= HandleContextChanged;
        // safety: ensure unbind even if derived forgot
        BindContinuousInputs(false);
    }

    // Override to hook into desired events from CorePlayer
    protected abstract void Subscribe(CorePlayer c);
    protected abstract void Unsubscribe(CorePlayer c);

    // ✅ Renamed to avoid collision with PlayerInput "OnMove" / "OnLook" send messages
    protected virtual void OnMoveContinuous(Vector2 move) { }
    protected virtual void OnLookContinuous(Vector2 look) { }

    protected bool IsContextAllowed()
    {
        if (allowedContexts == null || allowedContexts.Length == 0) return true;
        foreach (var ctx in allowedContexts)
            if (core.CurrentContext == ctx) return true;
        return false;
    }

    private void HandleContextChanged(string newCtx)
    {
        OnContextChanged(newCtx);
    }

    protected virtual void OnContextChanged(string newContext) { }

    // Helper for derived classes to attach/detach to continuous events if they want them
    protected void BindContinuousInputs(bool bind)
    {
        if (core == null) return;

        if (bind)
        {
            core.OnMove += OnMoveContinuous;
            core.OnLook += OnLookContinuous;
        }
        else
        {
            core.OnMove -= OnMoveContinuous;
            core.OnLook -= OnLookContinuous;
        }
    }
}
