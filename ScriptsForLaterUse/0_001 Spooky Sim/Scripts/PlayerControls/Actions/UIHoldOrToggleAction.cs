using UnityEngine;
using UnityEngine.InputSystem; // New Input System

/// <summary>
/// PlayerAction that shows a UI only while Tab is held, or when a "sticky" toggle
/// is enabled via Caps Lock. Visibility is also gated by PlayerAction.IsContextAllowed().
/// Attach this to your UI root (Canvas). A CanvasGroup is used to show/hide & block raycasts.
/// </summary>
[DisallowMultipleComponent]
public class UIHoldOrToggleAction : PlayerAction
{
    [Header("Target")]
    [Tooltip("CanvasGroup on the UI root to show/hide. If left empty, one will be found or added.")]
    [SerializeField] public GameObject uiGroup;

    [Header("Behavior")]
    [Tooltip("If true, cursor becomes visible when the UI shows, and is hidden/locked when the UI hides.")]
    [SerializeField] private bool manageCursor = true;

    [Tooltip("Start with the sticky toggle ON (useful for debugging menus).")]
    [SerializeField] private bool startStickyOn = false;

    [Tooltip("If true, clears the sticky toggle when the context becomes disallowed.")]
    [SerializeField] private bool resetStickyOnContextExit = false;

    // Internal state
    private bool _sticky;        // toggled by Caps Lock
    private bool _currentShown;  // last applied visible state

    // ------------ PlayerAction lifecycle ------------
    protected override void Awake()
    {
        base.Awake();

        if (uiGroup == null)
        {
            uiGroup = GameObject.Find("UIPanel");
        }

        _sticky = startStickyOn;
        ApplyVisible(false); // start hidden until first update decides otherwise
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateVisibilityImmediate();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // Ensure it's hidden when disabled
        ApplyVisible(false);
    }

    // No CorePlayer event hooks needed for this action
    protected override void Subscribe(CorePlayer c) { /* no-op */ }
    protected override void Unsubscribe(CorePlayer c) { /* no-op */ }

    protected override void OnContextChanged(string newContext)
    {
        // If context becomes disallowed, hide and optionally clear sticky
        if (!IsContextAllowed())
        {
            if (resetStickyOnContextExit) _sticky = false;
            ApplyVisible(false);
        }
        else
        {
            UpdateVisibilityImmediate();
        }
    }

    // ------------ Runtime logic ------------
    private void Update()
    {
        // Gate by context
        bool contextOK = IsContextAllowed();

        var kb = Keyboard.current;
        bool hold = kb != null && kb.tabKey.isPressed;

        // Toggle sticky on Caps Lock press (edge trigger)
        if (kb != null && kb.capsLockKey.wasPressedThisFrame)
            _sticky = !_sticky;

        bool shouldShow = contextOK && (hold || _sticky);

        if (shouldShow != _currentShown)
            ApplyVisible(shouldShow);
    }

    /// <summary>Forces a visibility recompute (e.g., if startStickyOn flipped in play mode).</summary>
    public void UpdateVisibilityImmediate()
    {
        var kb = Keyboard.current;
        bool hold = kb != null && kb.tabKey.isPressed;
        bool shouldShow = IsContextAllowed() && (hold || _sticky);
        ApplyVisible(shouldShow);
    }

    /// <summary>Public API: set sticky state from other scripts.</summary>
    public void SetSticky(bool on)
    {
        _sticky = on;
        UpdateVisibilityImmediate();
    }

    /// <summary>Public API: query if currently shown.</summary>
    public bool IsVisible => _currentShown;

    // ------------ Helpers ------------
    private void ApplyVisible(bool show)
    {
        _currentShown = show;

        if (uiGroup == null)
        {
            uiGroup = GameObject.Find("UIPanel");
        }

        uiGroup.SetActive(show);

        if (manageCursor)
        {
            Cursor.visible = show;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
