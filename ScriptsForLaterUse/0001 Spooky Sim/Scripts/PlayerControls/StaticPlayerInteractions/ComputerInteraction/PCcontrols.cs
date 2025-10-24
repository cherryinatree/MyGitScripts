using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

public enum ClickKind { Single, Double }

/// <summary>
/// In-game computer mouse controller.
/// - Moves a UI cursor within a RectTransform "screen".
/// - Keeps hotspot corner inside the screen.
/// - Finds a PCClickable under the hotspot and sends Single/Double clicks.
/// - Reads click from hardware mouse by default, but movement comes from CorePlayer.OnLook (via PlayerAction).
/// </summary>
public class PControls : PlayerAction
{
    public enum HotspotCorner { TopLeft, TopRight, BottomLeft, BottomRight, Center, TopCenter }
    public enum HitTestMode { Physics2D, Physics3D, UIRectContains, Auto }

    [Header("Screen & Cursor")]
    [Tooltip("World-space (or Screen-Space) RectTransform that represents the in-game monitor area.")]
    [SerializeField] private RectTransform screenArea;
    [Tooltip("RectTransform for your cursor graphic (must be a child of screenArea).")]
    [SerializeField] private RectTransform cursor;
    [Tooltip("Which corner of the cursor is the 'hotspot' (the click point).")]
    [SerializeField] private HotspotCorner hotspotCorner = HotspotCorner.TopLeft;

    [Header("Movement")]
    [Tooltip("Pixels (or canvas units) per second per 1.0 of look delta.")]
    [SerializeField] private float cursorSpeed = 1000f;
    [Tooltip("Invert axes if desired.")]
    [SerializeField] private bool invertX = false, invertY = true; // look deltas often come inverted on Y

    [Header("Hit Testing")]
    [Tooltip("How to detect what the hotspot is touching.")]
    [SerializeField] private HitTestMode hitTest = HitTestMode.Auto;
    [Tooltip("Physics2D/3D layer(s) considered clickable.")]
    [SerializeField] private LayerMask clickableMask = ~0;
    [Tooltip("If using UIRectContains, camera used for ScreenSpace-Camera canvas (optional).")]
    [SerializeField] private Camera uiCamera;

    [Header("Clicks")]
    [Tooltip("Max seconds between two presses to count as a double-click.")]
    [SerializeField] private float doubleClickThreshold = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool showDebugHotspot = false;
    [SerializeField] private Transform debugHotspotMarker; // optional visual

    // State
    private PCClickable currentHover;
    private float lastPrimaryClickTime = -999f;
    private bool singleClickPending = false;
    private Coroutine singleClickCo;



    [Header("Input System")]
    [SerializeField] private InputActionReference clickAction; // assign in Inspector

    // --- PlayerAction hooks ---
    protected override void Subscribe(CorePlayer c)
    {
        // We want continuous look deltas for movement
        BindContinuousInputs(true);
    }

    protected override void Unsubscribe(CorePlayer c)
    {
        BindContinuousInputs(false);
    }

    protected override void OnLookContinuous(Vector2 look)
    {
        if (!IsContextAllowed() || screenArea == null || cursor == null) return;

        // Convert look delta into cursor motion
        Vector2 delta = look;
        if (invertX) delta.x = -delta.x;
        if (invertY) delta.y = -delta.y;

        // Unscaled so mouse feels snappy (change to Time.deltaTime if you want timescale)
        Vector2 move = delta * cursorSpeed * Time.unscaledDeltaTime;
        Vector2 pos = cursor.anchoredPosition + move;

        // Clamp so HOTSPOT remains inside the screen rect
        pos = ClampAnchoredForHotspot(pos);

        cursor.anchoredPosition = pos;
    }

    private void Update()
    {
        if (!IsContextAllowed() || screenArea == null || cursor == null) return;

        // Visualize hotspot (optional)
        if (showDebugHotspot && debugHotspotMarker != null)
        {
            debugHotspotMarker.position = HotspotWorldPosition();
        }

        // Hover detection each frame
        UpdateHover();

    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (clickAction != null)
        {
            clickAction.action.performed += OnClickPerformed;
            clickAction.action.Enable();
        }
    }

    protected override void OnDisable()
    {
        if (clickAction != null)
        {
            clickAction.action.performed -= OnClickPerformed;
            clickAction.action.Disable();
        }
        base.OnDisable();
    }
    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        // If you set Interaction = Press Only, this will be called once on press.
        if (!ctx.performed) return;

        // optional extra guard if you didn’t set Press Only:
        if (!ctx.ReadValueAsButton()) return;

        HandlePrimaryPressed(); // <-- call your existing single/double-click logic
    }
    private void HandlePrimaryPressed()
    {
        float t = Time.unscaledTime;
        bool isDouble = (t - lastPrimaryClickTime) <= doubleClickThreshold;

        if (isDouble && singleClickPending)
        {
            singleClickPending = false;
            if (singleClickCo != null) StopCoroutine(singleClickCo);
            FireClick(ClickKind.Double);
        }
        else
        {
            lastPrimaryClickTime = t;
            // delay single click slightly to allow double-click upgrade
            singleClickPending = true;
            if (singleClickCo != null) StopCoroutine(singleClickCo);
            singleClickCo = StartCoroutine(CoDelayedSingleClick());
        }
    }

    private System.Collections.IEnumerator CoDelayedSingleClick()
    {
        yield return new WaitForSecondsRealtime(doubleClickThreshold);
        if (singleClickPending)
        {
            singleClickPending = false;
            FireClick(ClickKind.Single);
        }
    }

    private void FireClick(ClickKind kind)
    {
        if (currentHover != null)
        {
            currentHover.HandleClick(kind);
        }
    }

    // -------- Hover / Hit testing --------

    private void UpdateHover()
    {
        PCClickable hit = GetClickableUnderHotspot();


        if (hit != currentHover)
        {
            if (currentHover != null) currentHover.SetHover(false);
            currentHover = hit;
            if (currentHover != null) currentHover.SetHover(true);
        }

        Debug.Log(currentHover);
    }

    private PCClickable GetClickableUnderHotspot()
    {
        Vector3 wPos = HotspotWorldPosition();

        // Auto mode tries Physics2D first if any 2D colliders exist at point; then 3D; then UI rects
        if (hitTest == HitTestMode.Physics2D || hitTest == HitTestMode.Auto)
        {
            var hits = Physics2D.OverlapPointAll(wPos, clickableMask);
            if (hits != null && hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    var c = hits[i].GetComponentInParent<PCClickable>();
                    if (c != null && c.enabled && c.gameObject.activeInHierarchy) return c;
                }
            }
            if (hitTest == HitTestMode.Physics2D) return null; // forced mode ends here
        }

        if (hitTest == HitTestMode.Physics3D || hitTest == HitTestMode.Auto)
        {
            const float tiny = 0.001f;
            var hits3 = Physics.OverlapSphere(wPos, tiny, clickableMask, QueryTriggerInteraction.Collide);
            if (hits3 != null && hits3.Length > 0)
            {
                for (int i = 0; i < hits3.Length; i++)
                {
                    var c = hits3[i].GetComponentInParent<PCClickable>();
                    if (c != null && c.enabled && c.gameObject.activeInHierarchy) return c;
                }
            }
            if (hitTest == HitTestMode.Physics3D) return null;
        }

        if (hitTest == HitTestMode.UIRectContains || hitTest == HitTestMode.Auto)
        {
            // Try to find any PCClickable whose RectTransform contains the hotspot (within the screen hierarchy)
            // This is O(n) but typically small; optimize later if needed
            PCClickable best = null;
            foreach (var c in screenArea.GetComponentsInChildren<PCClickable>(includeInactive: false))
            {
                if (c.TryRectContains(HotspotScreenPoint(), uiCamera))
                {
                    best = c;
                    break;
                }
            }
            return best;
        }

        return null;
    }

    // -------- Hotspot math & clamping --------

    private Vector2 ClampAnchoredForHotspot(Vector2 anchored)
    {
        Rect r = screenArea.rect;
        Vector2 hs = HotspotLocalOffset(); // local offset from cursor pivot to hotspot

        // We must keep (anchored + hs) inside r
        float minX = r.xMin - hs.x;
        float maxX = r.xMax - hs.x;
        float minY = r.yMin - hs.y;
        float maxY = r.yMax - hs.y;

        anchored.x = Mathf.Clamp(anchored.x, minX, maxX);
        anchored.y = Mathf.Clamp(anchored.y, minY, maxY);
        return anchored;
    }

    private Vector2 HotspotLocalOffset()
    {
        // Offset from cursor pivot to a chosen corner
        var rect = cursor.rect;
        Vector2 size = rect.size;
        Vector2 pivot = cursor.pivot;

        // Local position of each corner relative to pivot (RectTransform local space)
        Vector2 topLeft = new Vector2(-size.x * pivot.x, size.y * (1f - pivot.y));
        Vector2 topRight = new Vector2(size.x * (1f - pivot.x), size.y * (1f - pivot.y));
        Vector2 bottomLeft = new Vector2(-size.x * pivot.x, -size.y * pivot.y);
        Vector2 bottomRight = new Vector2(size.x * (1f - pivot.x), -size.y * pivot.y);
        Vector2 center = Vector2.zero;
        Vector2 topCenter = new Vector2(0, size.y * (1f - pivot.y));

        return hotspotCorner switch
        {
            HotspotCorner.TopLeft => topLeft,
            HotspotCorner.TopRight => topRight,
            HotspotCorner.BottomLeft => bottomLeft,
            HotspotCorner.BottomRight => bottomRight,
            HotspotCorner.Center => center,
            HotspotCorner.TopCenter => topCenter,
            _ => topLeft
        };
    }

    private Vector3 HotspotWorldPosition()
    {
        // Convert (anchoredPosition + hotspotLocal) into world position
        Vector2 local = cursor.anchoredPosition + HotspotLocalOffset();
        return screenArea.TransformPoint(local);
    }

    private Vector2 HotspotScreenPoint()
    {
        // For UIRectContains mode, convert hotspot world -> screen
        Vector3 wp = HotspotWorldPosition();
        var cam = uiCamera != null ? uiCamera : Camera.main;
        return cam != null ? (Vector2)cam.WorldToScreenPoint(wp) : (Vector2)wp;
    }
}
