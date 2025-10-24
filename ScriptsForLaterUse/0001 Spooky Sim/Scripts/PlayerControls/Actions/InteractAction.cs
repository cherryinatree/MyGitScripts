using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player interaction: raycast + proximity triggers. Requires CorePlayer.
/// Uses raycast first; falls back to closest valid proximity target.
/// </summary>
[RequireComponent(typeof(CorePlayer))]
public class InteractAction : PlayerAction
{
    [Header("Raycast")]
    [SerializeField] private Transform rayOrigin;           // usually the camera
    [SerializeField] private float rayDistance = 3.0f;
    [SerializeField] private LayerMask rayMask = ~0;

    [Header("Proximity")]
    [SerializeField] private float proximityMaxDistance = 2.0f;

    [Header("Targeting")]
    [Tooltip("If true, keep highlighting the best target each frame.")]
    [SerializeField] private bool enableFocus = true;

    private readonly HashSet<Interactable> _proximity = new();
    private Interactable _focused;

    protected void Start()
    {
        base.Awake();
        if (rayOrigin == null && Camera.main) rayOrigin = Camera.main.transform;
    }

    protected override void Subscribe(CorePlayer c)
    {
        c.OnInteractStarted += TryInteract;
        // optional: keep focus updated
        BindContinuousInputs(true);
    }

    protected override void Unsubscribe(CorePlayer c)
    {
        c.OnInteractStarted -= TryInteract;
        BindContinuousInputs(false);
    }

    protected override void OnLookContinuous(Vector2 look)
    {
        if (!enableFocus) return;
        UpdateFocus();
    }
    protected override void OnMoveContinuous(Vector2 move)
    {
        if (!enableFocus) return;
        UpdateFocus();
    }

    private void Update()
    {
        if (!enableFocus) return;
        UpdateFocus();
    }

    private void UpdateFocus()
    {
        var best = FindBestTarget();
        if (best == _focused) return;

        if (_focused) _focused.OnFocusLost(core.gameObject);
        _focused = best;
        if (_focused) _focused.OnFocusGained(core.gameObject);
    }

    private void TryInteract()
    {
        var target = FindBestTarget();
        if (target == null) return;
        if (!target.CanInteract(core.gameObject)) return;

        target.Interact(core.gameObject);
    }

    private Interactable FindBestTarget()
    {
        Interactable best = null;
        float bestScore = float.NegativeInfinity;

        // 1) Raycast (highest priority if it hits)
        if (rayOrigin != null)
        {
            if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, rayDistance, rayMask, QueryTriggerInteraction.Collide))
            {
                Debug.DrawLine(rayOrigin.position, hit.point, Color.green, 0.1f);
                if (hit.collider && hit.collider.TryGetComponent<Interactable>(out var it1))
                {
                    if (it1.CanInteract(core.gameObject))
                        return it1; // raycast wins immediately
                }
                // try parent
                var itParent = hit.collider.GetComponentInParent<Interactable>();
                if (itParent && itParent.CanInteract(core.gameObject))
                    return itParent;
            }
        }

        // 2) Proximity set: choose the closest valid one in front-ish of the player
        Vector3 origin = core.transform.position;
        Vector3 fwd = rayOrigin ? rayOrigin.forward : core.transform.forward;

        foreach (var it in _proximity)
        {
            if (!it || !it.isActiveAndEnabled) continue;
            if (!it.CanInteract(core.gameObject)) continue;

            Vector3 to = it.FocusPoint - origin;
            float dist = to.magnitude;
            if (dist > proximityMaxDistance) continue;

            float forwardDot = Vector3.Dot(fwd.normalized, to.normalized); // front weighting
            float score = (1f - Mathf.Clamp01(dist / proximityMaxDistance)) + Mathf.Clamp01(forwardDot);

            if (score > bestScore)
            {
                bestScore = score;
                best = it;
            }
        }

        return best;
    }

    // ---- called by Interactable via trigger enter/exit ----
    public void RegisterProximity(Interactable it)
    {
        if (it != null) _proximity.Add(it);
    }
    public void UnregisterProximity(Interactable it)
    {
        if (it != null)
        {
            _proximity.Remove(it);
            if (_focused == it)
            {
                _focused.OnFocusLost(core.gameObject);
                _focused = null;
            }
        }
    }
}
